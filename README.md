# nuget-multi-push

DotNet core tool for pushing a directory full of packages to a nuget server rather than looping through all the packages in the folder one at a time.

## Installation

``dotnet tool install Credfeto.Package.Push``

## Usage

``dotnet pushpackages -folder c:\packages --api-key API-KEY --source https://feed-push-url/``

