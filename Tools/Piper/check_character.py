"""Integration check for worker duration control and explicit hesitation silence."""
import base64
import json
import subprocess
import sys
from pathlib import Path


def main():
    root = Path(__file__).resolve().parents[2]
    process = subprocess.Popen(
        [sys.executable, '-u', str(root / 'Tools/Piper/worker.py'),
         '--model', str(root / 'Models/Piper/ru_RU-irina-medium.onnx')],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True, encoding='utf-8')
    try:
        assert json.loads(process.stdout.readline())['kind'] == 'ready'

        def synth(request_id, scale, pause):
            request = dict(id=request_id, command='speak', text='Ой, сейчас очки найду.',
                           length_scale=scale, pause_seconds=pause)
            process.stdin.write(json.dumps(request) + '\n')
            process.stdin.flush()
            duration, chunks = 0.0, 0
            while True:
                reply = json.loads(process.stdout.readline())
                assert reply['kind'] not in ('error', 'fatal'), reply
                if reply['kind'] == 'done':
                    return duration, chunks
                if reply['kind'] == 'audio':
                    pcm = base64.b64decode(reply['audio'])
                    frame_bytes = reply['channels'] * 2
                    duration += len(pcm) / frame_bytes / reply['sample_rate']
                    chunks += 1
                    if pause:
                        silence_bytes = int(reply['sample_rate'] * pause) * frame_bytes
                        assert pcm[-silence_bytes:] == bytes(silence_bytes)

        fast, _ = synth(1, .8, 0)
        slow, _ = synth(2, 1.3, 0)
        paused, chunks = synth(3, 1.0, .2)
        assert slow > fast * 1.2, (fast, slow)
        assert chunks >= 2, 'Expected an interjection pause'
        print(f'PASS: fast={fast:.2f}s, slow={slow:.2f}s; hesitation chunks={chunks}, silence verified; total={paused:.2f}s')
    finally:
        process.terminate()
        process.wait(timeout=10)


if __name__ == '__main__':
    main()
