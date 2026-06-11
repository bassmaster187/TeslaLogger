xcopy ..\docs docs\ /YE
xcopy ..\img static\img\ /YE
xcopy ..\docs\en i18n\en\docusaurus-plugin-content-docs\current\ /YE
rmdir /s /q docs/en

REM npm install
REM npm run 
npm run build
pause
