using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.CommonTests;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp.Tests
{
    [TestClass]
    public class WindowsStorageFolderTests : CommonIModifiableFolderTests
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

        // Windows Shell change notifications can be delayed under concurrent test load.
        public override int PropertyWatcherTimeoutMs => 10000;

        // UWP doesn't support setting folder timestamps - they're read-only
        public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFolder?>(null);
        public override Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFolder?>(null);
        public override Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFolder?>(null);
        public override Task<IFile?> CreateFileInFolderWithLastModifiedAtAsync(IModifiableFolder folder, DateTime lastModifiedAt) => Task.FromResult<IFile?>(null);
        public override Task<CommonIModifiableFolderTests.CreateFileInFolderWithTimestampsResult?> CreateFileInFolderWithTimestampsAsync(IModifiableFolder folder, DateTime? createdAt, DateTime? lastModifiedAt, DateTime? lastAccessedAt) => Task.FromResult<CommonIModifiableFolderTests.CreateFileInFolderWithTimestampsResult?>(null);
    }
}
