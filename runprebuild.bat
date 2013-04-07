@ECHO OFF

echo ====================================
echo ==== AURORA  BUILDING ==============
echo ====================================
echo.

rem ## Default architecture (86 (for 32bit), 64, AnyCPU)
set bits=AnyCPU

rem ## Whether or not to add the .net3.5 flag
set framework=4_0

rem ## Default "configuration" choice ((r)elease, (d)ebug)
set configuration=d

rem ## Default "run compile batch" choice (y(es),n(o))
set compile_at_end=y

echo I will now ask you four questions regarding your build.
echo However, if you wish to build for:
echo        %bits% Architecture
echo        .NET %framework%
if %compile_at_end%==y echo And you would like to compile straight after prebuild...
echo.
echo Simply tap [ENTER] three times.
echo.
echo Note that you can change these defaults by opening this
echo batch file in a text editor.
echo.

:bits
set /p bits="Choose your architecture (AnyCPU, x86, x64) [%bits%]: "
if %bits%==86 goto configuration
if %bits%==x86 goto configuration
if %bits%==64 goto configuration
if %bits%==x64 goto configuration
if %bits%==AnyCPU goto configuration
echo "%bits%" isn't a valid choice!
goto bits

:configuration
set /p configuration="Choose your configuration ((r)elease or (d)ebug)? [%configuration%]: "
if %configuration%==r goto framework
if %configuration%==d goto framework
if %configuration%==release goto framework
if %configuration%==debug goto framework
echo "%configuration%" isn't a valid choice!
goto configuration

:framework
set /p framework="Choose your .NET framework (4_0 or 4_5)? [%framework%]: "
if %framework%==4_0 goto final
if %framework%==4_5 goto final
echo "%framework%" isn't a valid choice!
goto framework

:final
echo.
echo.

if exist Compile.*.bat (
    echo Deleting previous compile batch file...
    echo.
    del Compile.*.bat
)

echo Calling Prebuild for target %vstudio% with framework %framework%...
bin\Prebuild.exe /target vs2010 /targetframework v%framework% /conditionals ISWIN;NET_%framework%

echo.
echo Creating compile batch file for your convinence...
set fpath=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\msbuild
if %bits%==x64 set args=/p:Platform=x64
if %bits%==x86 set args=/p:Platform=x86
if %configuration%==r  (
    set cfg=/p:Configuration=Release
    set configuration=release
)
if %configuration%==d  (
set cfg=/p:Configuration=Debug
set configuration=debug
)
if %configuration%==release set cfg=/p:Configuration=Release
if %configuration%==debug set cfg=/p:Configuration=Debug
set filename=Compile.VS2010.net%framework%.%bits%.%configuration%.bat

echo %fpath% Aurora.sln %args% %cfg% > %filename% /p:DefineConstants="ISWIN;NET_%framework%"

echo.
set /p compile_at_end="Done, %filename% created. Compile now? (y,n) [%compile_at_end%]"
if %compile_at_end%==y (
    %filename%
    pause
)