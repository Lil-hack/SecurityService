param([string]$Python = 'python')
$ErrorActionPreference = 'Stop'
$gemmaRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$gemmaEnvironment = Join-Path $gemmaRoot '.local\gemma-runtime'
& $Python -c "import sys; assert sys.version_info >= (3, 10), 'Python 3.10+ required'"
if ($LASTEXITCODE -ne 0) { throw 'Use Python 3.10 or newer.' }
& $Python -m venv --without-pip $gemmaEnvironment
if ($LASTEXITCODE -ne 0) { throw 'Could not create the local Python environment.' }
& $Python -m pip --python (Join-Path $gemmaEnvironment 'Scripts\python.exe') install -r (Join-Path $PSScriptRoot 'requirements.txt')
if ($LASTEXITCODE -ne 0) { throw 'LiteRT-LM installation failed. Ensure the base Python has a recent pip.' }
Write-Output 'LiteRT-LM installed. Open Assets/Scenes/GemmaChat.unity in Unity.'
