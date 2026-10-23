"""Synthesize each extracted Mantella voice, record compatibility and WAV previews."""
import contextlib
import gc
import json
from pathlib import Path
import time
import wave

import numpy as np
from piper import PiperVoice, SynthesisConfig


def main():
    root = Path(__file__).resolve().parents[2]
    pack = root / 'Models/Piper/Mantella-RU-Piper-1.0'
    output = root / '.local/piper-mantella-previews'
    output.mkdir(parents=True, exist_ok=True)
    results = []
    for model in sorted(pack.rglob('*.onnx')):
        started = time.monotonic()
        try:
            voice = PiperVoice.load(str(model), use_cuda=False)
            chunks = list(voice.synthesize('Здравствуйте. Это проверка русского голоса.',
                                          syn_config=SynthesisConfig(length_scale=1.1)))
            if not chunks:
                raise RuntimeError('No audio generated')
            pcm = b''.join(chunk.audio_int16_bytes for chunk in chunks)
            samples = np.frombuffer(pcm, dtype='<i2').astype(np.float32) / 32768
            rms = float(np.sqrt(np.mean(samples ** 2)))
            if not np.isfinite(rms) or rms < .001:
                raise RuntimeError('Silent or invalid audio')
            rate, channels = chunks[0].sample_rate, chunks[0].sample_channels
            with wave.open(str(output / (model.stem + '.wav')), 'wb') as wav:
                wav.setnchannels(channels); wav.setsampwidth(2); wav.setframerate(rate)
                wav.writeframes(pcm)
            result = dict(voice=model.stem, ok=True, sample_rate=rate, rms=rms,
                          audio_seconds=len(samples) / rate / channels,
                          elapsed_seconds=round(time.monotonic() - started, 2))
            del voice, chunks
            gc.collect()
        except Exception as error:
            result = dict(voice=model.stem, ok=False, error=str(error))
        results.append(result)
        print(json.dumps(result), flush=True)
    (pack / 'compatibility-check.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
    if len(results) != 22 or not all(item['ok'] for item in results):
        raise RuntimeError('Some voices failed, see compatibility-check.json')
    print('PASS: all 22 Mantella voices synthesized non-silent speech', flush=True)


if __name__ == '__main__':
    main()
