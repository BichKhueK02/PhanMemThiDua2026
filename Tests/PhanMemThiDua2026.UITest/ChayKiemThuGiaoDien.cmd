@echo off
chcp 65001 >nul
rem Build Release (ca phan mem me) roi chay bo bam thu giao dien ngay trong bin\Release\net8.0-windows.
rem Phan mem that phai dang TAT. Bao cao: bin\Release\net8.0-windows\Database\NhatKyNhaPhatTrien\uitest-*.log
cd /d "%~dp0"
dotnet build "PhanMemThiDua2026.UITest.csproj" -c Release -v q -nologo
if errorlevel 1 (
  echo Build loi.
  pause
  exit /b 1
)
start "" "%~dp0..\..\bin\Release\net8.0-windows\PhanMemThiDua2026.UITest.exe"
