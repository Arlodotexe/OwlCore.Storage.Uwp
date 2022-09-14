
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.CommonTests;
using Windows.Storage;

namespace OwlCore.Storage.Uwp.Tests
{
    [TestClass]
    public class WindowsStorageFileTests : CommonIFileTests
    {
        public override async Task<IFile> CreateFileAsync()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var file = await folder.CreateFileAsync("test", CreationCollisionOption.GenerateUniqueName);

            return new WindowsStorageFile(file);
        }
    }
}
