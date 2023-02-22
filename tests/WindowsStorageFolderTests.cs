using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.CommonTests;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp.Tests
{
    [TestClass]
    public class WindowsStorageFolderTests : IModifiableFolderTests
    {

        public override async Task<IModifiableFolder> CreateModifiableFolderAsync()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var newFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName).AsTask();

            return new WindowsStorageFolder(newFolder);
        }

        public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
        {
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var newFolder = await tempFolder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName).AsTask();

            var folder = new WindowsStorageFolder(newFolder);

            for (int i = 0; i < fileCount; i++)
                await folder.CreateFileAsync($"{Guid.NewGuid()}");

            for (int i = 0; i < folderCount; i++)
                await folder.CreateFolderAsync($"{Guid.NewGuid()}");

            return folder;
        }
    }
}
