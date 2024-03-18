
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Memory;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp.Tests
{
    [TestClass]
    public class WindowsStorageFileTests : CommonIFileTests
    {
        public override async Task<IFile> CreateFileAsync()
        {
            var folder = (IModifiableFolder)new WindowsStorageFolder(ApplicationData.Current.TemporaryFolder);
            var randomDataFile = await CreateFileWithRandomDataAsync();

            return await folder.CreateCopyOfAsync(randomDataFile, overwrite: true);
        }

        async Task<MemoryFile> CreateFileWithRandomDataAsync()
        {
            var randomData = GenerateRandomData(256_000);
            using var tempStr = new MemoryStream(randomData);
            
            var memoryStream = new MemoryStream();
            await tempStr.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            return new MemoryFile(memoryStream);

            static byte[] GenerateRandomData(int length)
            {
                var rand = new Random();
                var b = new byte[length];
                rand.NextBytes(b);

                return b;
            }
        }
    }
}
