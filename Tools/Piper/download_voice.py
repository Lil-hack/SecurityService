"""Install Irina Medium and config with upstream LFS integrity verification."""
import hashlib
import json
from pathlib import Path
import urllib.request

REPOSITORY = 'rhasspy/piper-voices'
DIRECTORY = 'ru/ru_RU/irina/medium'
NAME = 'ru_RU-irina-medium'
ROOT = Path(__file__).resolve().parents[2]


def read_json(url):
    with urllib.request.urlopen(url, timeout=30) as response:
        return json.load(response)


def main():
    revision = read_json('https://huggingface.co/api/models/' + REPOSITORY + '?expand[]=sha')['sha']
    listing = read_json('https://huggingface.co/api/models/' + REPOSITORY + '/tree/' + revision + '/' + DIRECTORY)
    files = {item['path'].rsplit('/', 1)[-1]: item for item in listing}
    output = ROOT / 'Models' / 'Piper'
    output.mkdir(parents=True, exist_ok=True)
    provenance = dict(repository=REPOSITORY, revision=revision, files={})
    for filename in [NAME + '.onnx', NAME + '.onnx.json', 'MODEL_CARD']:
        entry = files[filename]
        url = 'https://huggingface.co/' + REPOSITORY + '/resolve/' + revision + '/' + entry['path']
        with urllib.request.urlopen(url, timeout=60) as response:
            contents = response.read()
        digest = hashlib.sha256(contents).hexdigest()
        if len(contents) != entry['size']:
            raise RuntimeError('Invalid size: ' + filename)
        if entry.get('lfs') and digest != entry['lfs']['oid']:
            raise RuntimeError('Invalid SHA-256: ' + filename)
        target = output / filename
        if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest() != digest:
            raise RuntimeError('Existing file differs, refusing overwrite: ' + str(target))
        target.write_bytes(contents)
        provenance['files'][filename] = dict(size=len(contents), sha256=digest)
        print(filename, len(contents), 'bytes, verified', flush=True)
    (output / 'voice-source.json').write_text(json.dumps(provenance, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
