@rem Использовать так: myfile.txt File.resources
@rem производит перегенерацию файла ресурсов при необходимости

@if not exist %2 @goto :gen

@set myvar=
@set itvar=

@FOR %%i IN (%2) DO @set MyVar=%%~ti
@FOR /F "tokens=1,2* delims= " %%i IN ('@echo %MyVar%') DO @FOR /F "tokens=1,2* delims=. " %%k IN ('@echo %%i') DO @set myvar=%%m.%%l.%%k.%%j

@FOR %%i IN (%1) DO @set itvar=%%~ti
@FOR /F "tokens=1,2* delims= " %%i IN ('@echo %itvar%') DO @FOR /F "tokens=1,2* delims=. " %%k IN ('@echo %%i') DO @set itvar=%%m.%%l.%%k.%%j

@if %myvar% lss %itvar% @goto :gen

@set myvar=
@set itvar=
@goto :eof



:gen
@echo Generate Resources
@resgen %1 %2



