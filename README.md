# OwlCore.Storage.Uwp [![Version](https://img.shields.io/nuget/v/OwlCore.Storage.Uwp.svg)](https://www.nuget.org/packages/OwlCore.Storage.Uwp)

Provides a Windows.Storage implementation of the OwlCore.Storage APIs.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Storage.Uwp). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Storage.Uwp
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Storage.Uwp

## Usage


### Folders

```cs
// Get the StorageFolder
var storageFolder = ApplicationData.Current.LocalFolder;

// Create the abstraction
var folder = new WindowsStorageFolder(storageFolder);
```

### Files

```cs
// Get the StorageFile
var storageFile = await GetStorageFile();

// Create the abstraction
var file = new WindowsStorageFolder(storageFile);
```

## Financing

We accept donations [here](https://github.com/sponsors/Arlodotexe) and [here](https://www.patreon.com/arlodotexe), and we do not have any active bug bounties.

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

All OwlCore code is licensed under the MIT License. OwlCore is licensed under the MIT License. See the [LICENSE](./src/LICENSE.txt) file for more details.
