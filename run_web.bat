@echo off
chcp 65001 >nul
title ĐẠO DỊCH HUYỀN CƠ PHÁP - WEB APP SERVER
echo =========================================================================
echo     ĐẠO DỊCH HUYỀN CƠ PHÁP - KHỞI ĐỘNG MÁY CHỦ WEB APP
echo =========================================================================
echo Đang khởi động máy chủ tại http://localhost:5050 ...
if exist "bin\tids_engine.exe" (
    bin\tids_engine.exe web 5050
) else if exist "tids_engine.exe" (
    tids_engine.exe web 5050
) else (
    echo Đang chạy qua dotnet...
    dotnet run --project TidsEngine.csproj -- web 5050
)
pause
