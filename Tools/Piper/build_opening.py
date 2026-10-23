"""Bake the configured greeting once, keeping live phone/pitch effects in Unity."""
import json
import math
from pathlib import Path
import wave
from piper import PiperVoice, SynthesisConfig


def main():
    root = Path(__file__).resolve().parents[2]
    prompts = root / 'Tools/Gemma/Prompts'
    catalog = json.loads((prompts / 'characters.json').read_text(encoding='utf-8-sig'))
    text = catalog['characters'][0]['openingText']
    for name in ('nina_voice.json', 'nina_voice_cartoon.json'):
        profile = json.loads((prompts / name).read_text(encoding='utf-8-sig'))
        factor = math.pow(2, profile['pitchSemitones'] / 12)
        voice = PiperVoice.load(str(root / profile['voicePath']), use_cuda=False)
        config = SynthesisConfig(length_scale=factor / (profile['ratePercent'] / 100))
        output = root / 'Assets/Resources' / (profile['openingAudio'] + '.wav')
        output.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(output), 'wb') as wav:
            initialized = False
            for chunk in voice.synthesize(text, syn_config=config):
                if not initialized:
                    wav.setnchannels(chunk.sample_channels)
                    wav.setsampwidth(2)
                    wav.setframerate(chunk.sample_rate)
                    initialized = True
                wav.writeframes(chunk.audio_int16_bytes)
        print(str(output), flush=True)


if __name__ == '__main__':
    main()
