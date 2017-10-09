
@echo off


set H=R:\KSP_1.3.1_dev
set GAMEDIR=DraftTwitchViewers

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"

xcopy /y /s  /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"
