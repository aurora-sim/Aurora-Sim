@ECHO OFF

echo ====================================
echo ==== AURORA ========================
echo ====================================
echo.

rem ## Default Course of Action (aurora,server,config,quit)
set choice=aurora

rem ## Auto-restart on exit/crash (y,n)
set auto_restart=y

rem ## Pause on crash/exit (y,n)
set auto_pause=y

echo Welcome to the Aurora launcher.
if %auto_restart%==y echo I am configured to automatically restart on exit.
if %auto_pause%==y echo I am configured to automatically pause on exit.
echo You can edit this batch file to change your default choices.
echo.
echo You have the following choices:
echo	- aurora: Launches Aurora
echo	- server: Launches Aurora Grid services
echo	- config: Launches the configurator to configure Aurora
echo	- quit: Quits
echo.

:action
set /p choice="What would you like to do? (aurora, server, config, quit) [%choice%]: "
if %choice%==aurora (
	set app="Aurora.exe"
	goto launchcycle
)
if %choice%==server (
	set app="Aurora.Server.exe"
	goto launchcycle
)
if %choice%==config (
	set app="Aurora.Configurator.exe"
	goto launchcycle
)
if %choice%==quit goto eof
if %choice%==q goto eof
if %choice%==exit goto eof

echo "%choice%" isn't a valid choice!
goto action


:launchcycle
echo.
echo Launching %app%...
%app%
if %auto_pause%==y pause
if %auto_restart%==y goto launchcycle

:eof
pause
