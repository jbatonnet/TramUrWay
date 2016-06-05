@echo off

set source=bin\Release\net.thedju.TramUrWay.apk
set target=net.thedju.TramUrWay-Repack.apk

set zip="C:\Program Files\7-Zip\7z.exe"
set jarsigner="C:\Program Files (x86)\Java\jdk1.7.0_55\bin\jarsigner.exe"
set zipalign="C:\Program Files (x86)\Android\android-sdk\build-tools\23.0.2\zipalign.exe"

:: Extract files to temp directory
del /Q /S bin\Repack >nul
echo Extracting current APK ...
mkdir bin\Repack
%zip% x %source% -obin\Repack >nul

:: Repack APK
echo Repacking APK ...
%zip% a -mx5 -tzip bin\Repack\Repack.apk .\bin\Repack\* >nul

:: Sign and align target APK
echo Signing APK ...
%jarsigner% -verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore ..\..\net.thedju.keystore bin\Repack\Repack.apk TramUrWay
%jarsigner% -verify -verbose -certs bin\Repack\Repack.apk >nul

echo Aligning APK ...
del %target%
%zipalign% -v 4 bin\Repack\Repack.apk %target% >nul

echo Done :)
pause