using OwlCore.Storage.System.IO;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace OwlCore.Storage.Uwp.Extensions
{
    public static partial class ThumbnailHelpers
    {
        public static async Task<StorageItemThumbnail> GetThumbnailAsync(this SystemFile file, ThumbnailMode mode)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetThumbnailAsync(mode);
        }

        public static async Task<StorageItemThumbnail> GetThumbnailAsync(this SystemFile file, ThumbnailMode mode, uint requestedSize)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetThumbnailAsync(mode, requestedSize);
        }

        public static async Task<StorageItemThumbnail> GetThumbnailAsync(this SystemFile file, ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetThumbnailAsync(mode, requestedSize, options);
        }
    }

    public partial class ThumbnailHelpers
    {
        public static async Task<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(this SystemFile file, ThumbnailMode mode)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetScaledImageAsThumbnailAsync(mode);
        }

        public static async Task<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(this SystemFile file, ThumbnailMode mode, uint requestedSize)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetScaledImageAsThumbnailAsync(mode, requestedSize);
        }

        public static async Task<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(this SystemFile file, ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(file.Path);
            return await storageFile.GetScaledImageAsThumbnailAsync(mode, requestedSize, options);
        }
    }
}
