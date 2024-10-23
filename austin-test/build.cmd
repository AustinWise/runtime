@REM @ECHO OFF
SETLOCAL

CD /D %~dp0

md managed

dotnet build -o managed

IF ERRORLEVEL 1 EXIT /B 1

..\artifacts\bin\coreclr\windows.x64.Debug\ilc-published\ilc.exe @Runtime.Base.ilc.rsp

IF ERRORLEVEL 1 EXIT /B 1

link.exe @Runtime.Base.link.rsp

IF ERRORLEVEL 1 EXIT /B 1

..\artifacts\bin\coreclr\windows.x64.Debug\ilc-published\ilc.exe @program.ilc.rsp

IF ERRORLEVEL 1 EXIT /B 1

link.exe @program.link.rsp
