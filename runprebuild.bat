@ECHO OFF

echo ====================================
echo ==== AURORA  BUILDING ==============
echo ====================================
echo.

rem ## Default Visual Studio choice (2008, 2010)
set vstudio=2010

rem ## Default .NET Framework (3_5, 4_0 (Unsupported on VS2008))
set framework=4_0

rem ## Default architecture (86 (for 32bit), 64, AnyCPU)
set bits=AnyCPU

rem ## Whether or not to add the .net3.5 flag
set conditionals=

rem ## Default "configuration" choice ((r)elease, (d)ebug)
set configuration=d

rem ## Default "run compile batch" choice (y(es),n(o))
set compile_at_end=y

echo I will now ask you three questions regarding your build.
echo However, if you wish to build for:
echo        Visual Studio %vstudio%
echo        .NET Framework %framework%
echo        %bits%x Architecture
if %compile_at_end%==y echo And you would like to compile straight after prebuild...
echo.
echo Simply tap [ENTER] four times.
echo.
echo Note that you can change these defaults by opening this
echo batch file in a text editor.
echo.

:vstudio
set /p vstudio="Choose your Visual Studio version (2008, 2010) [%vstudio%]: "
if %vstudio%==2008 goto framework
if %vstudio%==2010 goto framework
echo "%vstudio%" isn't a valid choice!
goto vstudio

:framework
set /p framework="Choose your .NET framework (3_5, 4_0 (Unsupported on VS2008)) [%framework%]: "
if %framework%==3_5 goto bits
if %framework%==4_0 goto frameworkcheck
echo "%framework%" isn't a valid choice!
goto framework

    :frameworkcheck
    if %vstudio%==2008 goto frameworkerror
    goto bits

    :frameworkerror
    echo Sorry! Visual Studio 2008 only supports 3_5.
    goto framework

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
if %configuration%==r goto final
if %configuration%==d goto final
if %configuration%==release goto final
if %configuration%==debug goto final
echo "%configuration%" isn't a valid choice!
goto configuration

:final
echo.
echo.

if exist Compile.*.bat (
    echo Deleting previous compile batch file...
    echo.
    del Compile.*.bat
)

if %framework%==3_5 set conditionals=NET_3_5
if %framework%==4_0 set conditionals=NET_4_0

echo Calling Prebuild for target %vstudio% with framework %framework%...
bin\Prebuild.exe /target vs%vstudio% /targetframework v%framework% /conditionals ISWIN;%conditionals%

echo.
echo Creating compile batch file for your convinence...
if %framework%==3_5 set fpath=C:\WINDOWS\Microsoft.NET\Framework\v3.5\msbuild
if %framework%==4_0 set fpath=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\msbuild
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
set filename=Compile.VS%vstudio%.net%framework%.%bits%.%configuration%.bat

echo %fpath% Aurora.sln %args% %cfg% > %filename% /p:DefineConstants="ISWIN;%conditionals%"

echo.
set /p compile_at_end="Done, %filename% created. Compile now? (y,n) [%compile_at_end%]"
if %compile_at_end%==y (
    %filename%
    pause
)