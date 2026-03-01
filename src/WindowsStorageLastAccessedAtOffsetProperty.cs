#if WINDOWS_UWP
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp;

/// <summary>
/// A modifiable storage property for the last accessed timestamp (with offset) of a Windows.Storage item.
/// </summary>
/// <remarks>
/// Uses Win32 <c>GetFileTime</c>/<c>SetFileTime</c> via <c>IStorageItemHandleAccess</c> for both read and write
/// to ensure fresh (non-cached) values are always returned.
/// </remarks>
public sealed class WindowsStorageLastAccessedAtOffsetProperty : SimpleModifiableStorageProperty<DateTimeOffset?>, IModifiableLastAccessedAtOffsetProperty
{
    private readonly IStorageItem _storageItem;

    /// <summary>
    /// Creates a new instance of <see cref="WindowsStorageLastAccessedAtOffsetProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="storageItem">The underlying Windows.Storage item.</param>
    public WindowsStorageLastAccessedAtOffsetProperty(IStorable owner, IStorageItem storageItem)
        : base(
            id: owner.Id + "/" + nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            name: nameof(ILastAccessedAtOffset.LastAccessedAtOffset),
            asyncGetter: ct => StorageItemHandleAccessHelper.GetLastAccessedAtAsync(storageItem, ct),
            asyncSetter: async (v, ct) =>
            {
                if (v is null)
                    throw new ArgumentNullException(nameof(v), "Cannot set last accessed time to null.");
                await StorageItemHandleAccessHelper.SetLastAccessedAtAsync(storageItem, v.Value, ct);
            })
    {
        _storageItem = storageItem;
    }

    /// <inheritdoc/>
    public override async Task<IStoragePropertyWatcher<DateTimeOffset?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        var watcher = new WindowsStoragePropertyWatcher<DateTimeOffset?>(this, _storageItem, "System.DateAccessed", async ct =>
        {
            return await StorageItemHandleAccessHelper.GetLastAccessedAtAsync(_storageItem, ct);
        });

        await watcher.InitializeAsync(cancellationToken);
        return watcher;
    }
}
#endif