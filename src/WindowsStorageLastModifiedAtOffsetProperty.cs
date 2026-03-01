#if WINDOWS_UWP
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp;

/// <summary>
/// A modifiable storage property for the last modified timestamp (with offset) of a Windows.Storage item.
/// </summary>
/// <remarks>
/// Uses Win32 <c>GetFileTime</c>/<c>SetFileTime</c> via <c>IStorageItemHandleAccess</c> for both read and write
/// to ensure fresh (non-cached) values are always returned.
/// </remarks>
public sealed class WindowsStorageLastModifiedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableLastModifiedAtOffsetProperty
{
    private readonly IStorageItem _storageItem;

    /// <summary>
    /// Creates a new instance of <see cref="WindowsStorageLastModifiedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="storageItem">The underlying Windows.Storage item.</param>
    public WindowsStorageLastModifiedAtOffsetProperty(IStorable owner, IStorageItem storageItem)
        : base(
            id: owner.Id + "/" + nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            name: nameof(ILastModifiedAtOffset.LastModifiedAtOffset),
            asyncGetter: ct => StorageItemHandleAccessHelper.GetLastModifiedAtAsync(storageItem, ct),
            asyncSetter: async (v, ct) =>
            {
                if (v is null)
                    throw new ArgumentNullException(nameof(v), "Cannot set last modified time to null.");
                await StorageItemHandleAccessHelper.SetLastModifiedAtAsync(storageItem, v.Value, ct);
            })
    {
        _storageItem = storageItem;
    }

    /// <inheritdoc/>
    public override async Task<IStoragePropertyWatcher<DateTimeOffset?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        var watcher = new WindowsStoragePropertyWatcher<DateTimeOffset?>(this, _storageItem, "System.DateModified", async ct =>
        {
            return await StorageItemHandleAccessHelper.GetLastModifiedAtAsync(_storageItem, ct);
        });

        await watcher.InitializeAsync(cancellationToken);
        return watcher;
    }
}
#endif