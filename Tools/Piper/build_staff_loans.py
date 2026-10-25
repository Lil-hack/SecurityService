"""Bake instant staff greetings in their four actual Piper voices (no network)."""
import argparse
import base64
import json
import subprocess
import sys
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
THINKING = 'Хм, дайте подумать. Мой кошелёк сейчас совещается с совестью.'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--manifest', type=Path, default=ROOT / 'Tools/Gemma/Prompts/office_staff_defaults.json')
    args = parser.parse_args()
    profiles = json.loads(args.manifest.read_text(encoding='utf-8'))
    folder = ROOT / 'Assets/Resources/Audio/StaffLoans'
    folder.mkdir(parents=True, exist_ok=True)
    for profile in profiles['personas']:
        model = ROOT / profile['voicePath']
        if not model.is_file() or not model.with_suffix('.onnx.json').is_file():
            raise FileNotFoundError(model)
        process = subprocess.Popen([sys.executable, '-u', str(ROOT / 'Tools/Piper/worker.py'), '--model', str(model)],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, encoding='utf-8',
            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        try:
            ready = json.loads(process.stdout.readline())
            if ready.get('kind') != 'ready':
                raise RuntimeError('Piper did not initialize')
            for index, (suffix, text) in enumerate([('greeting', profile['greeting']), ('paid', profile['alreadyPaidGreeting']), ('thinking', THINKING)]):
                request = dict(command='speak', id=index+1, text=text, length_scale=2**(profile['pitchSemitones']/12)/profile['speechRate'], pause_seconds=.04)
                process.stdin.write(json.dumps(request, ensure_ascii=False)+'\n');process.stdin.flush()
                audio, rate, channels = [], None, None
                for line in process.stdout:
                    reply = json.loads(line)
                    if reply.get('kind') == 'audio':
                        audio.append(base64.b64decode(reply['audio']));rate=reply['sample_rate'];channels=reply['channels']
                    elif reply.get('kind') == 'done':
                        break
                    elif reply.get('kind') in ('error', 'fatal'):
                        raise RuntimeError('Voice synthesis failed')
                if not audio:
                    raise RuntimeError('No PCM generated')
                path = folder / (profile['id']+'_'+suffix+'.wav')
                with wave.open(str(path), 'wb') as wav:
                    wav.setnchannels(channels);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes(b''.join(audio))
                print(path.name, round(sum(map(len,audio))/2/channels/rate,2), 'seconds', flush=True)
        finally:
            try: process.stdin.write('{"command":"quit","id":0}\n');process.stdin.flush();process.wait(timeout=5)
            except (OSError, subprocess.TimeoutExpired): process.kill();process.wait()
    (folder/'manifest.json').write_text(json.dumps(profiles,ensure_ascii=False,indent=2),encoding='utf-8')


if __name__ == '__main__':
    main()
