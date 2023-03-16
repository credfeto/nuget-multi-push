# nuget-multi-push

DotNet core tool for pushing a directory full of packages to a nuget server rather than looping through all the packages
in the folder one at a time.

## Installation

``dotnet tool install Credfeto.Package.Push``

## Usage

``dotnet pushpackages --folder c:\packages --api-key API-KEY --source https://feed-push-url/``

## Build Status

| Branch  | Status                                                                                                                                                                                                                                                |
|---------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| main    | [![Build: Pre-Release](https://github.com/credfeto/nuget-multi-push/actions/workflows/build-and-publish-pre-release.yml/badge.svg)](https://github.com/credfeto/nuget-multi-push/actions/workflows/build-and-publish-pre-release.yml) |
| release | [![Build: Release](https://github.com/credfeto/nuget-multi-push/actions/workflows/build-and-publish-release.yml/badge.svg)](https://github.com/credfeto/nuget-multi-push/actions/workflows/build-and-publish-release.yml)             |

## Changelog

View [changelog](CHANGELOG.md)
