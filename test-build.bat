@echo off
echo Testing Angular compilation...
pushd "src\frontend\postal-idempotency-app"
echo Current directory: %CD%
echo.
echo Running npm run build...
npm run build 2>&1
echo.
echo Build completed. Check output above for any errors.
popd
echo.
echo Testing .NET compilation...
dotnet build "d:\2025 Stu\jpPostIdempotencyDemo\jpPostIdempotencyDemo.sln"
echo.
echo .NET build completed. Check output above for any errors.
pause
