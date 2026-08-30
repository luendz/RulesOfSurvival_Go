@echo off
setlocal

set "ROS_UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"

if not exist "%ROS_UNITY_EXE%" (
    echo No se encontro Unity 6000.3.11f1 en:
    echo %ROS_UNITY_EXE%
    pause
    exit /b 1
)

echo Abriendo RulesOfSurvival_Go con Direct3D 11...
start "" "%ROS_UNITY_EXE%" -projectPath "%~dp0" -force-d3d11

endlocal
