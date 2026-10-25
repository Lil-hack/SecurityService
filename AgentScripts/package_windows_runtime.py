"""Package the existing local Windows AI runtime; never copy account credentials."""
import argparse
import os
from pathlib import Path
import shutil

PROJECT = Path(__file__).resolve().parents[1]


def copy_tree(source, target, excluded=()):
    source, target = Path(source), Path(target)
    for folder, directories, files in os.walk(str(source)):
        directories[:] = [d for d in directories if d not in excluded]
        output = target / Path(folder).relative_to(source)
        output.mkdir(parents=True, exist_ok=True)
        for name in files:
            if name.endswith(('.pyc', '.pyo')):
                continue
            shutil.copy2(str(Path(folder) / name), str(output / name))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', default='Builds/Windows')
    parser.add_argument('--with-model', action='store_true')
    args = parser.parse_args()
    output = (PROJECT / args.output).resolve()
    if PROJECT / 'Builds' not in output.parents:
        raise ValueError('Output must be a subdirectory of this project\'s Builds folder.')
    environment = PROJECT / '.local/gemma-runtime'
    config = dict(line.split(' = ', 1) for line in (environment / 'pyvenv.cfg').read_text().splitlines() if ' = ' in line)
    base = Path(config['home'])
    if not (base / 'python.exe').is_file():
        raise FileNotFoundError('Base Python is missing.')
    runtime = output / '.local/gemma-runtime'
    scripts = runtime / 'Scripts'
    scripts.mkdir(parents=True, exist_ok=True)
    # This is a relocated full interpreter, not a venv tied to the developer's machine.
    for name in ('python.exe', 'python3.dll', 'python312.dll', 'vcruntime140.dll', 'vcruntime140_1.dll'):
        shutil.copy2(str(base / name), str(scripts / name))
    shutil.copy2(str(base / 'LICENSE.txt'), str(runtime / 'PYTHON-LICENSE.txt'))
    copy_tree(base / 'DLLs', runtime / 'DLLs', ('__pycache__',))
    copy_tree(base / 'Lib', runtime / 'Lib', ('site-packages', '__pycache__', 'test', 'tests', 'idlelib', 'tkinter', 'ensurepip'))
    copy_tree(environment / 'Lib/site-packages', runtime / 'Lib/site-packages', ('__pycache__', 'tests', 'test', 'pip', 'pip-25.0.1.dist-info'))
    for relative in ('Tools/Gemma/worker.py', 'Tools/Gemma/trust_evaluator.py', 'Tools/Gemma/hiring_evaluator.py', 'Tools/Gemma/jev_evaluator.py', 'Tools/Gemma/social_evaluator.py', 'Tools/Gemma/Prompts/office_boss.txt', 'Tools/Piper/worker.py', 'Tools/Piper/VOICE_MODEL_CARD.md'):
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(str(PROJECT / relative), str(destination))
    voice_root = 'Models/Piper/Mantella-RU-Piper-1.0'
    voices = ('malecommoner', 'maleyoungeager', 'femaleyoungeager', 'maleeventoned', 'femalecondescending')
    suffixes = [f'SKSE/Plugins/MantellaSoftware/piper/models/skyrim/low/{voice}.onnx{ext}' for voice in voices for ext in ('', '.json')]
    for suffix in suffixes + ['README-Mantella-RU-Piper.txt']:
        relative = voice_root + '/' + suffix
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(str(PROJECT / relative), str(destination))
    if args.with_model:
        (output / 'Models').mkdir(parents=True, exist_ok=True)
        for suffix in ('', '.verified.json'):
            name = 'gemma-4-E2B-it.litertlm' + suffix
            source = PROJECT / 'Models' / name
            if source.exists():
                shutil.copy2(str(source), str(output / 'Models' / name))
    (output / 'START-HERE.txt').write_text(
        'SCAMPLAY - Windows x64\n\n'
        'Run SecurityService.exe. Keep all adjacent folders and DLLs together.\n'
        'The main menu verifies the Gemma model and downloads it if missing.\n'
        'Settings are saved automatically. Co-op is not available yet.\n'
        'The local Python/Gemma/Piper runtime, boss voice and four staff voices are included.\n'
        'No private TypeSafe credentials are included. Jev uses TYPESAFE_API_KEY\n'
        'from the environment if configured; otherwise the existing local Gemma\n'
        'fallback evaluates interview answers.\n', encoding='utf-8')
    print('Packaged runtime: ' + str(output), flush=True)


if __name__ == '__main__':
    main()
