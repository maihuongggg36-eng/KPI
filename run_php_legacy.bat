@echo off
chcp 65001 > nul
echo Khởi động bản PHP cũ tại http://localhost:8000 ...
"C:\xampp\php\php.exe" -S localhost:8000 -t public
pause
