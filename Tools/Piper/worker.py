"""Persistent offline Piper CPU voice; PCM16 chunks over JSON-lines stdio."""
import argparse
import base64
import contextlib
import json
import re
import sys
from pathlib import Path


def emit(kind, request_id=0, **fields):
    print(json.dumps(dict(kind=kind, id=request_id, **fields), ensure_ascii=True), flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--model', required=True)
    args = parser.parse_args()
    try:
        with contextlib.redirect_stdout(sys.stderr):
            from piper import PiperVoice, SynthesisConfig
            voice = PiperVoice.load(args.model, use_cuda=False)
        emit('ready', text=Path(args.model).stem + ' · русский · CPU')
        for line in sys.stdin:
            request_id = 0
            try:
                request = json.loads(line)
                request_id = int(request['id'])
                if request.get('command') == 'quit':
                    break
                text = request.get('text', '').replace('\\n', '\n').strip()
                # Do not read markdown formatting or simple stage directions aloud.
                text = re.sub(r'\*[^*\n]+\*', '', text)
                text = re.sub(r'[`#*_]', '', text).strip()
                if not text:
                    emit('done', request_id)
                    continue
                length_scale = max(0.35, min(2.0, float(request.get('length_scale', 1.1))))
                pause_seconds = max(0.0, min(0.5, float(request.get('pause_seconds', 0.0))))
                config = SynthesisConfig(length_scale=length_scale)
                # Hesitation pauses after interjections already present in the reply.
                # Never add invented words or read stage directions aloud.
                if pause_seconds > 0:
                    text = re.sub(r'\b(ой|ох|ах|ну|господи)([,!…])\s*', r'\1\2\n', text, flags=re.IGNORECASE)
                with contextlib.redirect_stdout(sys.stderr):
                    for segment in text[:6000].splitlines():
                        if not segment.strip():
                            continue
                        for chunk in voice.synthesize(segment, syn_config=config):
                            # Silence is part of the same clip, so stop/cancel also clears pauses.
                            pcm = chunk.audio_int16_bytes
                            pcm += bytes(int(chunk.sample_rate * pause_seconds) * chunk.sample_channels * 2)
                            with contextlib.redirect_stdout(sys.__stdout__):
                                emit('audio', request_id,
                                     sample_rate=chunk.sample_rate,
                                     channels=chunk.sample_channels,
                                     audio=base64.b64encode(pcm).decode('ascii'))
                emit('done', request_id)
            except Exception as error:
                emit('error', request_id, text=str(error))
    except Exception as error:
        emit('fatal', text=str(error))
        return 1
    return 0


if __name__ == '__main__':
    sys.stdin.reconfigure(encoding='utf-8')
    sys.stdout.reconfigure(encoding='utf-8')
    sys.exit(main())
