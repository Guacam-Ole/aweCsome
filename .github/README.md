# AweCsome Framework
AweCsome Framework: Entity Framework for SharePoint Provider Hosted Add-Ins. If you need a quick Overview what it does and if it can help you, read the [Why use AweCsome?](/help/about.md) document.

## Issues
If you find a bug or have a feature request, feel free to place an issue here. If you need support, please use [Stackoverflow](https://stackoverflow.com) or [SharePoint SE](https://sharepoint.stackexchange.com) instead.

I will look there even more often than here. And if you have a reputation of 1500 or above. Feel free to create an "aweCsome" tag :)

## Using AweCsome
You can install AweCsome using NuGet. You always need to install two packages: [AweCsome.Interfaces](https://www.nuget.org/packages/AweCsome.Interfaces) and the package for the SharePoint-Version you are using, e.g. [AweCsome.O365](https://www.nuget.org/packages/AweCsome.O365) or [AweCsome.2013](https://www.nuget.org/packages/AweCsome.2013)

## Building AweCsome
If you want to build the source by yourself using Visual Studio you need to update the nuget - packages to met your local structure.

Simply type `Update-Package -reinstall` and you should be ready to go.

## Getting Help
Documentation can be found at the **Wiki**

## Build status:
### Develop (CI)
* Interfaces 
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Debug/AweCsome-Interfaces-Develop?branchName=develop)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=7&branchName=develop)

* SharePoint O365
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Debug/AweCsome-365-Develop?branchName=develop)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=5&branchName=develop)

* SharePoint 2013
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Debug/AweCsome-2013-Develop?branchName=develop)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=6&branchName=develop)

### Master (stable)
* Interfaces
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Release/AweCsome-Interfaces-Master?branchName=master)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=9&branchName=master)

* SharePoint O365
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Release/AweCsome-365-Master?branchName=master)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=10&branchName=master)

* SharePoint 2013
[![Build Status](https://olealbers.visualstudio.com/AweCsome/_apis/build/status/Release/AweCsome-2013-Master?branchName=master)](https://olealbers.visualstudio.com/AweCsome/_build/latest?definitionId=11&branchName=master)

## NuGet-Packages
All AweCsome-Packages are available as NuGet-Packages. They can be found here:

[AweCsome.Interfaces](https://www.nuget.org/packages/AweCsome.Interfaces/)

[AweCsome.O365](https://www.nuget.org/packages/AweCsome.O365/)

[AweCSome.2013](https://www.nuget.org/packages/AweCsome.2013/)
