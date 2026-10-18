"""Download the portable E2B LiteRT bundle; verify its upstream LFS SHA-256."""
import hashlib
import json
from pathlib import Path
import urllib.request

REPO = 'litert-community/gemma-4-E2B-it-litert-lm'
NAME = 'gemma-4-E2B-it.litertlm'
ROOT = Path(__file__).resolve().parents[2]


def main():
    with urllib.request.urlopen('https://huggingface.co/api/models/' + REPO + '?blobs=true') as response:
        metadata = json.load(response)
    entry = next(item for item in metadata['siblings'] if item['rfilename'] == NAME)
    expected = entry['lfs']['sha256']
    size = entry['size']
    revision = metadata['sha']
    folder = ROOT / 'Models'
    folder.mkdir(exist_ok=True)
    target = folder / NAME
    if target.exists():
        with target.open('rb') as model:
            digest = hashlib.file_digest(model, 'sha256').hexdigest()
        if digest == expected:
            print('Model already installed and verified.', flush=True)
            return
        raise RuntimeError('Existing model has a different hash; it was not overwritten.')
    temporary = target.with_suffix('.litertlm.partial')
    digest = hashlib.sha256()
    print('Downloading', NAME, size, 'bytes; revision', revision, flush=True)
    with urllib.request.urlopen('https://huggingface.co/' + REPO + '/resolve/' + revision + '/' + NAME, timeout=60) as response:
        with temporary.open('wb') as output:
            total = 0
            progress = 0
            while True:
                chunk = response.read(4 * 1024 * 1024)
                if not chunk:
                    break
                output.write(chunk)
                digest.update(chunk)
                total += len(chunk)
                if total - progress >= 256 * 1024 * 1024:
                    print(str(round(total / size * 100)) + '%', flush=True)
                    progress = total
    if total != size or digest.hexdigest() != expected:
        raise RuntimeError('Model integrity check failed; partial file retained for inspection.')
    temporary.replace(target)
    (folder / 'gemma-model-source.json').write_text(json.dumps(dict(
        repository=REPO, revision=revision, filename=NAME, sha256=expected,
        size=size), indent=2), encoding='utf-8')
    print('Verified SHA-256. Model installed:', target, flush=True)


if __name__ == '__main__':
    main()
