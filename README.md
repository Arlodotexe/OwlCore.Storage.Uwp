# OwlCore.Storage.Uwp [![Version](https://img.shields.io/nuget/v/OwlCore.Storage.Uwp.svg)](https://www.nuget.org/packages/OwlCore.Storage.Uwp)

Provides a Windows.Storage implementation of the OwlCore.Storage APIs.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Storage.Uwp). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Storage.Uwp
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Storage.Uwp

## Usage


### Folders
For folders, we've implemented `IFile` and `IAddressableFile` as `WindowsStorageFile`.

```cs
// Get the StorageFolder
var storageFolder = ApplicationData.Current.LocalFolder;

// Create the abstraction
var folder = new WindowsStorageFolder(storageFolder);
```

### Files
For files, we've implemented `IFolder`, `IAddressableFolder`, `IFolderCanFastGetItem`, and `IModifiableFolder`.

```cs
// Get the StorageFile
var storageFile = await GetStorageFile();

// Create the abstraction
var file = new WindowsStorageFolder(storageFile);
```

## Financing

We accept donations, and we do not have any active bug bounties.

If you’re looking to contract a new project, new features, improvements or bug fixes, please contact me. 

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

We’re using the MIT license for 3 reasons:
1. We're here to share useful code. You may use any part of it freely, as the MIT license allows. 
2. A library is no place for viral licensing.
3. Easy code transition to larger community-based projects.

