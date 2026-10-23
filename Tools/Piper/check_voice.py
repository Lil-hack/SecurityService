"""Real CPU synthesis smoke test. Writes .local/piper-check.wav, never mock audio."""
import array
import base64
import json
import math
from pathlib import Path
import queue
import subprocess
import sys
import threading
import wave


def main():
    root = Path(__file__).resolve().parents[2]
    process = subprocess.Popen(
        [sys.executable, '-u', str(root / 'Tools/Piper/worker.py'),
         '--model', str(root / 'Models/Piper/ru_RU-irina-medium.onnx')],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True, encoding='utf-8', bufsize=1)
    replies = queue.Queue()

    def receive():
        for line in process.stdout:
            replies.put(json.loads(line))

    threading.Thread(target=receive, daemon=True).start()
    try:
        ready = replies.get(timeout=40)
        assert ready['kind'] == 'ready', ready
        process.stdin.write(json.dumps(dict(id=1, text='Алло? Это Нина Петровна. Ой, милок, мне пришло тревожное сообщение!')) + '\n')
        process.stdin.flush()
        pcm = bytearray()
        rate = channels = 0
        while True:
            reply = replies.get(timeout=40)
            if reply['kind'] == 'audio':
                rate, channels = reply['sample_rate'], reply['channels']
                pcm.extend(base64.b64decode(reply['audio']))
            elif reply['kind'] == 'done':
                break
            else:
                raise RuntimeError(reply)
        samples = array.array('h', pcm)
        rms = math.sqrt(sum((sample / 32768) ** 2 for sample in samples) / len(samples))
        assert channels == 1 and rate == 22050 and rms > .001, (rate, channels, rms)
        output = root / '.local/piper-check.wav'
        with wave.open(str(output), 'wb') as wav:
            wav.setnchannels(channels)
            wav.setsampwidth(2)
            wav.setframerate(rate)
            wav.writeframes(pcm)
        print(json.dumps(dict(sample_rate=rate, channels=channels,
                              seconds=len(samples) / rate / channels, rms=rms,
                              path=str(output))))
    finally:
        process.terminate()


if __name__ == '__main__':
    main()
