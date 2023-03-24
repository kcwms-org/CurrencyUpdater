# Setting UP the Kevcoder.CurrencyUpdateService

## Build
1. On Premises
    1. run dotnet build .\FXService\FXService.csproj --configuration Release  
    1. run dotnet publish .\FXService\FXService.csproj --nologo --output artifact
1. On Github
    1. the workflow (defined in publish-service.yml) will, **on every push to master**, build and produce an artifact.zip which you can download

## Deploy

## Manage The Windows Service

1. Install
    1. copy contents of artifact.zip(github build) or the artifact folder(on premises build) to C:\Services\Kevcoder
    1. run sc.exe create SRW.CurrencyUpdater binpath=  C:\Services\Kevcoder.CurrencyUpdater\Kevcoder.FXService.exe start= auto
1. Update
    1. run sc.exe stop SRW.CurrencyUpdater 
    1. copy contents of artifact.zip(github build) or the artifact folder(on premises build) to C:\Services\Kevcoder
    1. run sc.exe start SRW.CurrencyUpdater 
    
1. Uninstall    
    1. sc.exe delete SRW.CurrencyUpdater 