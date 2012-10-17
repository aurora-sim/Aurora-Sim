@ECHO OFF

echo ====================================
echo ==== AURORA BUILDING .NET 3.5 ======
echo ====================================
echo.

rem ## Default Visual Studio choice (2008, 2010)
set vstudio=2008

rem ## Default .NET Framework (3_5, 4_0 (Unsupported on VS2008))
set framework=3_5

rem ## Default architecture (86 (for 32bit), 64, AnyCPU)
set bits=AnyCPU

rem ## Whether or not to add the .net3.5 flag
set conditionals=

rem ## Default "configuration" choice ((r)elease, (d)ebug)
set configuration=d

rem ## Default "run compile batch" choice (y(es),n(o))
set compile_at_end=n

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