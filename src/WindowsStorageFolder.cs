using OwlCore.Storage.SystemIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace OwlCore.Storage.Uwp
{
    /// <summary>
    /// An implementation of <see cref="IFolder"/> for <see cref="Windows.Storage.StorageFolder"/>.
    /// </summary>
    public class WindowsStorageFolder : IFolder, IAddressableFolder, IFolderCanFastGetItem, IModifiableFolder
    {
        /// <summary>
        /// Creates a new instance of <see cref="WindowsStorageFolder"/>.
        /// </summary>
        public WindowsStorageFolder(IStorageFolder storageFolder)
        {
            StorageFolder = storageFolder;
        }

        /// <summary>
        /// The folder being wrapped.
        /// </summary>
        internal IStorageFolder StorageFolder { get; }

        /// <inheritdoc/>
        public string Path => StorageFolder.Path;

        /// <inheritdoc/>
        public string Id => Path;

        /// <inheritdoc/>
        public string Name => StorageFolder.Name;

        /// <inheritdoc/>
        public async Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // We use paths as the ID. Extract the file name.
            var fileName = System.IO.Path.GetFileName(id);

            var item = await StorageFolder.GetItemAsync(fileName);

            cancellationToken.ThrowIfCancellationRequested();

            if (item is IStorageFile file)
                return new WindowsStorageFile(file);

            if (item is IStorageFolder folder)
                return new WindowsStorageFolder(folder);

            throw new FileNotFoundException();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type == StorableType.None)
                throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

            if (type.HasFlag(StorableType.All))
            {
                var items = await StorageFolder.GetItemsAsync();

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item is IStorageFolder folder)
                        yield return new WindowsStorageFolder(folder);

                    if (item is IStorageFile file)
                        yield return new WindowsStorageFile(file);
                }

                yield break;
            }

            if (type.HasFlag(StorableType.Folder))
            {
                var folders = await StorageFolder.GetFoldersAsync().AsTask();

                foreach (var folder in folders)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new WindowsStorageFolder(folder);
                }
            }

            if (type.HasFlag(StorableType.File))
            {
                var files = await StorageFolder.GetFilesAsync().AsTask();

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new WindowsStorageFile(file);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (StorageFolder is IStorageItem2 folder)
                return new WindowsStorageFolder(await folder.GetParentAsync());

            throw new NotSupportedException($"{nameof(GetParentAsync)} is only supported when the provided {nameof(IStorageFile)} implementation also implements {nameof(IStorageItem2)}.");
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use provided platform methods where possible.
            if (fileToCopy is WindowsStorageFile windowsFile)
            {
                var storageFile = await windowsFile.StorageFile.CopyAsync(StorageFolder, desiredNewName: fileToCopy.Name, option: overwrite ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists);
                return new WindowsStorageFile(storageFile);
            }
            else if (fileToCopy is SystemFile systemFile)
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(systemFile.Path);
                return await CreateCopyOfAsync(new WindowsStorageFile(storageFile), overwrite, cancellationToken);
            }
            else
            {
                // Manual file copy. Slower, but covers all other scenarios.
                using var sourceStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);

                if (sourceStream.CanSeek)
                    sourceStream.Seek(0, SeekOrigin.Begin);

                var storageFile = await StorageFolder.CreateFileAsync(fileToCopy.Name);
                using var destinationStream = await storageFile.OpenStreamForWriteAsync();

                await sourceStream.CopyToAsync(destinationStream, bufferSize: 81920, cancellationToken);

                return new WindowsStorageFile(storageFile);
            }
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use provided system methods where possible.
            if (fileToMove is WindowsStorageFile storageFile)
            {
                await storageFile.StorageFile.MoveAsync(StorageFolder);
                return fileToMove;
            }

            // Manual move. Slower, but covers all other scenarios.
            var newFile = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
            await source.DeleteAsync(fileToMove, cancellationToken);

            return newFile;
        }

        /// <inheritdoc/>
        public async Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var storageFile = await StorageFolder.CreateFileAsync(name, overwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.FailIfExists);

            return new WindowsStorageFile(storageFile);
        }

        /// <inheritdoc/>
        public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var storageFolder = await StorageFolder.CreateFolderAsync(name, overwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.FailIfExists);

            return new WindowsStorageFolder(storageFolder);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parent = await item.GetParentAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (parent is null || parent.Id != Id)
                throw new FileNotFoundException("The provided item was not found in the folder");

            if (item is WindowsStorageFile file)
                await file.StorageFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask();

            if (item is WindowsStorageFolder folder)
                await folder.StorageFolder.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask();
        }

        /// <inheritdoc/>
        public virtual async Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (StorageFolder is IStorageFolderQueryOperations folder)
            {
                var knownItems = await folder.GetItemsAsync(startIndex: 0, maxItemsToRetrieve: uint.MaxValue);
                cancellationToken.ThrowIfCancellationRequested();
                return new WindowsStorageFolderWatcher(folder, knownItems);
            }

            throw new NotSupportedException($"{nameof(GetFolderWatcherAsync)} is only supported when the provided {nameof(IStorageFile)} implementation also implements {nameof(IStorageFolderQueryOperations)}.");
        }
    }
}
