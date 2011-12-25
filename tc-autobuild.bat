@echo off

set makearch=
set makedist=

rem Set flags according to passed command line params
:ParamLoop
IF "%1"=="" GOTO ParamContinue
IF "%1"=="arch" set makearch=yes
IF "%1"=="dist" set makedist=yes
SHIFT
GOTO ParamLoop
:ParamContinue

rem use .NET 3.5 to build
bin\Prebuild.exe /target vs2008 /targetframework v3_5
IF ERRORLEVEL 1 GOTO FAIL

%WINDIR%\Microsoft.NET\Framework\v3.5\msbuild /t:Rebuild Aurora.sln
IF ERRORLEVEL 1 GOTO FAIL

IF NOT "%makearch%"=="yes" GOTO SkipArch
echo Build success, creating zip package
del /q aurora-autobuild.zip
7z -tzip a aurora-autobuild.zip bin
:SkipArch

:SUCCESS
exit /B 0

:FAIL
exit /B 1
