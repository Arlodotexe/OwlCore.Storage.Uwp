using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp;

/// <summary>
/// An implementation of <see cref="IFile"/> for <see cref="Windows.Storage.StorageFile"/>.
/// </summary>
public class WindowsStorageFile : IFile, IChildFile
#if WINDOWS_UWP
    , ICreatedAtOffset, ILastModifiedAtOffset, ILastAccessedAtOffset
#endif
{
#if WINDOWS_UWP
    private WindowsStorageCreatedAtProperty? _createdAt;
    private WindowsStorageCreatedAtOffsetProperty? _createdAtOffset;
    private WindowsStorageLastModifiedAtProperty? _lastModifiedAt;
    private WindowsStorageLastModifiedAtOffsetProperty? _lastModifiedAtOffset;
    private WindowsStorageLastAccessedAtProperty? _lastAccessedAt;
    private WindowsStorageLastAccessedAtOffsetProperty? _lastAccessedAtOffset;
#endif

    /// <summary>
    /// Creates a new instance of <see cref="WindowsStorageFile"/>.
    /// </summary>
    public WindowsStorageFile(IStorageFile storageFile)
    {
        StorageFile = storageFile;
    }

    /// <summary>
    /// The file being wrapped.
    /// </summary>
    public IStorageFile StorageFile { get; }

    /// <inheritdoc/>
    public string Id => Path;

    /// <inheritdoc/>
    public string Name => StorageFile.Name;

    /// <summary>
    /// The system path to the file.
    /// </summary>
    public string Path => StorageFile.Path;

#if WINDOWS_UWP
    /// <inheritdoc/>
    public ICreatedAtProperty CreatedAt => _createdAt ??= new WindowsStorageCreatedAtProperty(this, StorageFile);

    /// <inheritdoc/>
    public ICreatedAtOffsetProperty CreatedAtOffset => _createdAtOffset ??= new WindowsStorageCreatedAtOffsetProperty(this, StorageFile);

    /// <inheritdoc/>
    public ILastModifiedAtProperty LastModifiedAt => _lastModifiedAt ??= new WindowsStorageLastModifiedAtProperty(this, StorageFile);

    /// <inheritdoc/>
    public ILastModifiedAtOffsetProperty LastModifiedAtOffset => _lastModifiedAtOffset ??= new WindowsStorageLastModifiedAtOffsetProperty(this, StorageFile);

    /// <inheritdoc/>
    public ILastAccessedAtProperty LastAccessedAt => _lastAccessedAt ??= new WindowsStorageLastAccessedAtProperty(this, StorageFile);

    /// <inheritdoc/>
    public ILastAccessedAtOffsetProperty LastAccessedAtOffset => _lastAccessedAtOffset ??= new WindowsStorageLastAccessedAtOffsetProperty(this, StorageFile);
#endif

    /// <inheritdoc/>
    public async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (StorageFile is IStorageItem2 file)
            return new WindowsStorageFolder(await file.GetParentAsync());

        throw new NotSupportedException($"{nameof(GetParentAsync)} is only supported when the provided {nameof(IStorageFile)} implementation also implements {nameof(IStorageItem2)}.");
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenStreamAsync(FileAccess accessMode = FileAccess.Read, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS_UWP
        // Windows.Storage async stream operations don't reliably update LastAccessTime.
        // Manually update BEFORE opening the stream to avoid sharing violations.
        // Any file open (read or write) is considered an access.
        await StorageItemHandleAccessHelper.SetLastAccessedAtAsync(StorageFile, DateTimeOffset.Now, cancellationToken);
#endif

        Stream stream;

        if (accessMode.HasFlag(FileAccess.Write) && accessMode.HasFlag(FileAccess.Read))
        {
            var randomAccessStream = await StorageFile.OpenAsync(FileAccessMode.ReadWrite).AsTask(cancellationToken);
            stream = randomAccessStream.AsStream();
        }
        else if (accessMode.HasFlag(FileAccess.Write))
        {
            stream = await StorageFile.OpenStreamForWriteAsync();
        }
        else if (accessMode.HasFlag(FileAccess.Read))
        {
            stream = await StorageFile.OpenStreamForReadAsync();
        }
        else
        {
            throw new ArgumentOutOfRangeException(paramName: nameof(accessMode), message: $"{accessMode} cannot be used here.");
        }

        return stream;
    }
}
