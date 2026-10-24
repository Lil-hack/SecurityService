"""Local JSON-lines bridge to Google's LiteRT-LM CPU runtime.

stdout is reserved for the protocol; diagnostics go to stderr.
"""
import argparse
import contextlib
import json
import os
import sys
import time
from trust_evaluator import evaluate
from hiring_evaluator import evaluate_hiring


def emit(kind, text='', **fields):
    print(json.dumps(dict(kind=kind, text=text, **fields), ensure_ascii=True), flush=True, file=sys.__stdout__)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--model', required=True)
    parser.add_argument('--system-prompt')
    parser.add_argument('--interview', action='store_true', help='Office interview prompts arrive with each JSON command')
    args = parser.parse_args()
    model_name = 'Gemma 4 E2B IT'
    if not os.path.isfile(args.model):
        emit('fatal', 'Файл модели не найден: ' + args.model)
        return 1
    persona_path = args.system_prompt or os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                'Prompts', 'nina_petrovna.txt')
    persona = ''
    if not args.interview:
        with open(persona_path, encoding='utf-8') as persona_file:
            persona = persona_file.read().strip()
    engine = None
    conversation = None
    recent_dialogue = []
    try:
        # Some Python dependencies print startup diagnostics. Keep them out of IPC.
        with contextlib.redirect_stdout(sys.stderr):
            import litert_lm
            litert_lm.set_min_log_severity(litert_lm.LogSeverity.ERROR)
            engine = litert_lm.Engine(args.model,
                                      backend=litert_lm.Backend.CPU(thread_count=4),
                                      max_num_tokens=4096)

        def new_conversation(opening=''):
            with contextlib.redirect_stdout(sys.stderr):
                system = persona
                if opening:
                    system += '\nТы уже начала разговор этой репликой: «' + opening + '». Не повторяй приветствие; дальше отвечай на слова собеседника.'
                return engine.create_conversation(
                    max_output_tokens=512,
                    messages=[litert_lm.Message.system(system)],
                    thinking_config=litert_lm.ThinkingConfig(enable_thinking=False))

        conversation = None if args.interview else new_conversation()
        emit('ready', model_name + ' · CPU · локально')
        for line in sys.stdin:
            try:
                request = json.loads(line)
                command = request.get('command')
                if command in ('reset', 'character'):
                    if command == 'character':
                        new_persona = request.get('text', '').strip()
                        if not new_persona:
                            raise ValueError('Не задана роль персонажа.')
                        persona = new_persona
                    with contextlib.redirect_stdout(sys.stderr):
                        conversation.close()
                    conversation = new_conversation()
                    recent_dialogue.clear()
                    emit('reset')
                elif command == 'seed_opening':
                    opening = request.get('text', '').strip()
                    with contextlib.redirect_stdout(sys.stderr):
                        conversation.close()
                    conversation = new_conversation(opening)
                    recent_dialogue[:] = [dict(role='character', text=opening[:1500])]
                    emit('opening_seeded')
                elif command == 'interview':
                    text = request.get('text', '').strip()[:1200]
                    if not text:
                        raise ValueError('Введите причину для приёма на работу.')
                    interview_started = time.perf_counter()
                    with contextlib.redirect_stdout(sys.stderr):
                        result = evaluate_hiring(engine, litert_lm, text, recent_dialogue,
                            on_sentence=lambda sentence: emit('interview_sentence', sentence),
                            prompt_settings=request.get('prompts'))
                    recent_dialogue.append(dict(role='user', text=text))
                    recent_dialogue.append(dict(role='character', text=result['text']))
                    del recent_dialogue[:-8]
                    result.setdefault('timings', {})['worker_total_ms'] = (time.perf_counter() - interview_started) * 1000
                    emit('interview_result', **result)
                elif command == 'evaluate':
                    text = request.get('text', '').strip()[:2000]
                    try:
                        with contextlib.redirect_stdout(sys.stderr):
                            result = evaluate(engine, litert_lm, text, recent_dialogue)
                    except Exception as error:
                        print('Trust evaluation unavailable: ' + str(error), file=sys.stderr)
                        result = dict(events=[], new_information=False, valid=False)
                    emit('evaluation', **result)
                elif command in ('send', 'start'):
                    prompt = request.get('text', '').strip()
                    if not prompt:
                        raise ValueError('Введите сообщение.')
                    dialogue_prompt = prompt
                    if request.get('trust', -1) >= 0:
                        trust = max(0, min(100, int(request['trust'])))
                        complete = request.get('mission_complete') is True
                        state = ('Миссия завершена. Спокойно скажи, что сама закончишь звонок и свяжешься с банком по официальному номеру с карты или из приложения.'
                                 if complete else 'Уровень доверия: ' + str(trust) + '/100. ' + request.get('state', '')[:160])
                        dialogue_prompt = ('[Состояние персонажа от игры: ' + state
                                           + ' Сохраняй правила безопасности. Не называй очки, миссию или оценку. '
                                           + 'Отвечай только прямой речью Нины.]\nРеплика собеседника:\n' + prompt)
                    answer_text = ''
                    with contextlib.redirect_stdout(sys.stderr):
                        stream = conversation.send_message_async(dialogue_prompt)
                    for chunk in stream:
                        for item in chunk.get('content', []):
                            if item.get('type') == 'text' and item.get('text'):
                                answer_text += item['text']
                                emit('token', item['text'])
                    if command == 'send':
                        recent_dialogue.append(dict(role='user', text=prompt[:2000]))
                    recent_dialogue.append(dict(role='character', text=answer_text[:1500]))
                    del recent_dialogue[:-8]
                    emit('done')
                elif command == 'quit':
                    break
                else:
                    raise ValueError('Неизвестная команда.')
            except Exception as error:
                # A failed generation may have partially mutated the KV cache.
                emit('fatal', str(error))
                return 1
    except Exception as error:
        emit('fatal', str(error))
        return 1
    finally:
        with contextlib.redirect_stdout(sys.stderr):
            if conversation is not None:
                conversation.close()
            if engine is not None:
                engine.close()
    return 0


if __name__ == '__main__':
    sys.stdin.reconfigure(encoding='utf-8')
    sys.stdout.reconfigure(encoding='utf-8')
    sys.exit(main())
