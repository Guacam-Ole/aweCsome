# aweCsome Framework
aweCsome Framework: Entity Framework for SharePoint Provider Hosted Add-Ins

## Issues
If you find a bug or have a feature request, feel free to place an issue here. If you need support, please use [Stackoverflow](https://stackoverflow.com) or [SharePoint SE](https://sharepoint.stackexchange.com) instead.

I will look there even more often than here. And if you have a reputation of 1500 or above. Feel free to create an "aweCsome" tag :)

## Using AweCsome
You can install AweCsome using NuGet. You always need to install two packages: [AweCsome.Interfaces](https://www.nuget.org/packages/AweCsome.Interfaces) and the package for the SharePoint-Version you are using, e.g. [AweCsome.O365](https://www.nuget.org/packages/AweCsome.O365) or [AweCsome.2013](https://www.nuget.org/packages/AweCsome.2013)

## Building AweCsome
If you want to build the source by yourself using Visual Studio you need to update the nuget - packages to met your local structure.

Simply type `Update-Package -reinstall` and you should be ready to go.

## Packaging AweCsome
To package this library into a NuGet-package, the latest NuGet CLI-version needs to be installed. You can download that from [https://www.nuget.org/downloads](https://www.nuget.org/downloads).

Before packaging a new release, the .nupsec-files needs to be updated. Specifically, the dependency-declarations need to be updated (if changed), the __version__-field needs to be bumped and the __releaseNotes__-field needs to be updated.

Then you can build the solutions (note that the __Release__-configuration needs to be selected in Visual Studio/MSBuild) and afterwards pack the respective libraries by running either `nuget pack nuget pack AweCsomeO365.nuspec` or `nuget pack nuget pack AweCsome2013.nuspec` from the __AweCsomeFramework__-folder.

This will generate the `AweCsome.O365.<VERSION>.nupkg` and `AweCsome.OnPremises.<VERSION>.nupkg` files respectively.
