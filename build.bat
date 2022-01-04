@echo off
set /p version="Enter Version Number to Build With: "

@echo on
dotnet pack ".\TomLonghurst.EnumerableAsyncProcessor\TomLonghurst.EnumerableAsyncProcessor.csproj"  --configuration Release /p:Version=%version%

pause