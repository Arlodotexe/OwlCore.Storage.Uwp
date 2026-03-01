#if WINDOWS_UWP
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp;

/// <summary>
/// A modifiable storage property for the creation timestamp of a Windows.Storage item.
/// </summary>
/// <remarks>
/// Uses Win32 <c>GetFileTime</c>/<c>SetFileTime</c> via <c>IStorageItemHandleAccess</c> for both read and write
/// to ensure fresh (non-cached) values are always returned.
/// </remarks>
public sealed class WindowsStorageCreatedAtProperty : SimpleModifiableStorageProperty<DateTime?>, IModifiableCreatedAtProperty
{
    private readonly IStorageItem _storageItem;

    /// <summary>
    /// Creates a new instance of <see cref="WindowsStorageCreatedAtProperty"/>.
    /// </summary>
    /// <param name="owner">The owning storable item.</param>
    /// <param name="storageItem">The underlying Windows.Storage item.</param>
    public WindowsStorageCreatedAtProperty(IStorable owner, IStorageItem storageItem)
        : base(
            id: owner.Id + "/" + nameof(ICreatedAt.CreatedAt),
            name: nameof(ICreatedAt.CreatedAt),
            asyncGetter: async ct =>
            {
                var value = await StorageItemHandleAccessHelper.GetCreatedAtAsync(storageItem, ct);
                return value?.LocalDateTime;
            },
            asyncSetter: async (v, ct) =>
            {
                if (v is null)
                    throw new ArgumentNullException(nameof(v), "Cannot set creation time to null.");
                await StorageItemHandleAccessHelper.SetCreatedAtAsync(storageItem, new DateTimeOffset(v.Value), ct);
            })
    {
        _storageItem = storageItem;
    }

    /// <inheritdoc/>
    public override async Task<IStoragePropertyWatcher<DateTime?>> GetWatcherAsync(CancellationToken cancellationToken)
    {
        var watcher = new WindowsStoragePropertyWatcher<DateTime?>(this, _storageItem, "System.DateCreated", async ct =>
        {
            var val = await StorageItemHandleAccessHelper.GetCreatedAtAsync(_storageItem, ct);
            return val?.LocalDateTime;
        });

        await watcher.InitializeAsync(cancellationToken);
        return watcher;
    }
}
#endif