@echo off

set EnableNuGetPackageRestore=true
set sln="backend\backend.sln"

path %path%;%cd%\tools;.\tools      
set col_stat=e
set col_err=c
set col_dbg=8
set col_proc=7
set col_ok=2

set netfx_dir_flag=0

echo Finding compiler...

:BUILD_WITH_NETFX
msbuild.exe /version >NUL 2>NUL
if %errorlevel% neq 0 if not "%netfx_dir_flag"=="0" goto FIND_NETFX
echo Compiling release build...
msbuild.exe %sln% /maxcpucount /toolsversion:4.0 /verbosity:m /nologo /p:Configuration=Release
if %ERRORLEVEL% NEQ 0 goto BUILD_ERROR
echo Compiling debug build...
msbuild.exe %sln% /maxcpucount /toolsversion:4.0 /verbosity:m /nologo /p:Configuration=Debug
if %ERRORLEVEL% NEQ 0 goto BUILD_ERROR
exit /B 0

:FIND_NETFX
dir /B "%windir%\Microsoft.NET\Framework\v4.*">.tmp
if %errorlevel% neq 0 goto BUILD_NOT
set /p dotnetversion=<.tmp
del .tmp
if "%dotnetversion%"=="" goto BUILD_NOT
set dotnetpath=%windir%\Microsoft.NET\Framework\%dotnetversion%
"%dotnetpath%\msbuild.exe" /version >NUL 2>NUL
if %errorlevel% neq 0 goto BUILD_NOT
path %path%;%dotnetpath%
echo .NET framework %dotnetversion% (not in path)
goto BUILD_WITH_NETFX

:: :BUILD_WITH_MONO
:: echo Trying to build with Mono...
:: :xbuild /p:TargetFrameworkProfile="" /verbosity:normal /nologo %sln%
:: if %ERRORLEVEL% NEQ 0 goto BUILD_NOT
:: exit /B

:BUILD_NOT
echo Could not find a working compiler.
echo [!] No working compiler found. Make sure a valid C# compiler and framework>&2
echo [!] are installed. If this message appears with Visual Studio 2010/2012 being>&2
echo [!] installed, try running this script from Visual Studio Command Line Tools.>&2
exit /B -1

:BUILD_ERROR
echo Build failed. |xecho /a:c
echo [!] The guessed compiler could not build the project. Make sure a valid C# compiler>&2
echo [!] and framework are installed. If this message appears with Visual Studio 2010/2012>&2
echo [!] being installed, try running this script from Visual Studio Command Line Tools.>&2
exit /B -1