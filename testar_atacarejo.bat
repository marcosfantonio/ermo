@echo off
rem ERMO - Atacarejo: gera modelos no Blender, liga prefabs, valida a planta
rem e joga os quatro caminhos da fase em Play Mode. Feche a Unity antes.
rem Outros caminhos: set UNITY=... e set BLENDER=... antes de rodar.
rem Resultado: Logs\atacarejo-status.txt e Logs\Atacarejo\*.png
setlocal
set "PROJ=%~dp0"
if "%PROJ:~-1%"=="\" set "PROJ=%PROJ:~0,-1%"
if not defined BLENDER set "BLENDER=C:\Program Files\Blender Foundation\Blender 5.3\blender.exe"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"
set "LOGS=%PROJ%\Logs"
set "STATUS=%LOGS%\atacarejo-status.txt"
if not exist "%LOGS%" mkdir "%LOGS%"
echo inicio %date% %time%> "%STATUS%"

if "%1"=="semblender" goto prefabs
echo [1/4] Blender: modelos do Atacarejo
"%BLENDER%" --background --factory-startup --python "%PROJ%\Assets\Ermo\Modelos\Atacarejo\gerar_atacarejo.py" -- todos > "%LOGS%\atacarejo-blender.log" 2>&1
if errorlevel 1 (echo blender FALHOU>> "%STATUS%" & goto fim)
echo blender ok>> "%STATUS%"

:prefabs
echo [2/4] Unity: materiais, prefabs e kit
start "" /wait "%UNITY%" -batchmode -projectPath "%PROJ%" -executeMethod Ermo.Editor.AtacarejoPrefabs.RebuildBatch -quit -logFile "%LOGS%\atacarejo-prefabs.log"
if errorlevel 1 (echo prefabs FALHOU>> "%STATUS%" & goto fim)
echo prefabs ok>> "%STATUS%"

echo [3/4] Unity: validacao da planta
start "" /wait "%UNITY%" -batchmode -projectPath "%PROJ%" -executeMethod Ermo.Editor.AtacarejoValidation.ValidateBatch -quit -logFile "%LOGS%\atacarejo-layout.log"
if errorlevel 1 (echo layout FALHOU>> "%STATUS%" & goto fim)
echo layout ok>> "%STATUS%"

echo [4/4] Unity: Play Mode: Sitio, mercado, camara 13, frigorifico
start "" /wait "%UNITY%" -batchmode -projectPath "%PROJ%" -executeMethod Ermo.Editor.AtacarejoPlayModeValidation.RunBatch -logFile "%LOGS%\atacarejo-play.log"
if errorlevel 1 (echo play FALHOU>> "%STATUS%") else (echo play ok>> "%STATUS%")
type "%LOGS%\Atacarejo\relatorio.txt" 2>nul

:fim
echo fim %date% %time%>> "%STATUS%"
endlocal
