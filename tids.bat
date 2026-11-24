@echo off
chcp 65001 >nul
if exist "bin\tids_engine.exe" (
    bin\tids_engine.exe %*
) else if exist "tids_engine.exe" (
    tids_engine.exe %*
) else (
    dotnet run --project TidsEngine.csproj -- %*
)
