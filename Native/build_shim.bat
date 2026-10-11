@echo off
setlocal
set "BIN=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231\bin\Hostx64\x64"
set "HERE=%~dp0"
set "OUT=%HERE%..\Dependencies"
if not exist "%OUT%" mkdir "%OUT%"
"%BIN%\ml64.exe" /nologo /c /Fo "%HERE%steam_api_init.obj" "%HERE%steam_api_init.asm"
if errorlevel 1 exit /b 1
"%BIN%\lib.exe" /nologo /MACHINE:X64 /DEF:"%HERE%kernel32.def" /OUT:"%HERE%kernel32.lib"
if errorlevel 1 exit /b 1
"%BIN%\link.exe" /nologo /DLL /NOENTRY /MACHINE:X64 /DEF:"%HERE%steam_api_init.def" "%HERE%steam_api_init.obj" "%HERE%kernel32.lib" /OUT:"%OUT%\steam_api_init.dll"
if errorlevel 1 exit /b 1
"%BIN%\link.exe" /nologo /DLL /NOENTRY /MACHINE:X64 /DEF:"%HERE%steam_api64.def" /OUT:"%OUT%\steam_api64.dll"
if errorlevel 1 exit /b 1
exit /b 0
