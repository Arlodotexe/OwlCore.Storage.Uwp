#if WINDOWS_UWP
using System;
using System.Diagnostics;
using System.Threading;
using OwlCore.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.FileProperties;

namespace OwlCore.Storage.Uwp;

internal class WindowsStoragePropertyWatcher<T> : IStoragePropertyWatcher<T>
{
    private readonly IStorageItem _item;
    private readonly string _propertySystemName;
    private readonly Func<CancellationToken, Task<T>> _valueGetter;
    private StorageItemQueryResult? _query;
    private T _lastKnownValue;

    public WindowsStoragePropertyWatcher(IStorageProperty<T> property, IStorageItem item, string propertySystemName, Func<CancellationToken, Task<T>> valueGetter)
    {
        Property = property;
        _item = item;
        _propertySystemName = propertySystemName;
        _valueGetter = valueGetter;
        _lastKnownValue = default!;
    }

    public IStorageProperty<T> Property { get; }

    public event EventHandler<T>? ValueUpdated;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StorageFolder? parent = null;
        if (_item is IStorageItem2 item2)
        {
            try
            {
                parent = await item2.GetParentAsync().AsTask(cancellationToken);
            }
            catch
            {
                // Parent might not be available (e.g. root folder, or permission issue)
            }
        }

        if (parent is null)
        {
            return;
        }

        var options = new QueryOptions(CommonFolderQuery.DefaultQuery);
        // Filter by name to target this specific item
        // Note: Using "System.ItemNameDisplay" AQS filter
        options.ApplicationSearchFilter = $"System.ItemNameDisplay:=\"{_item.Name}\"";
        
        // Monitor the specific property
        options.SetPropertyPrefetch(PropertyPrefetchOptions.None, new[] { _propertySystemName });

        _query = parent.CreateItemQueryWithOptions(options);
        _query.ContentsChanged += Query_ContentsChanged;

        // Ensure query is running
        await _query.GetItemCountAsync().AsTask(cancellationToken);
        
        // Initialize last known value
        _lastKnownValue = await _valueGetter(cancellationToken);
    }

    private async void Query_ContentsChanged(IStorageQueryResultBase sender, object args)
    {
        Debug.WriteLine($"[WindowsStoragePropertyWatcher] Query_ContentsChanged fired for {_item.Name}, property {_propertySystemName}");
        Logger.LogInformation($"Query_ContentsChanged fired for {_item.Name}, property {_propertySystemName}");
        try
        {
            var newValue = await _valueGetter(CancellationToken.None);
            Debug.WriteLine($"[WindowsStoragePropertyWatcher] newValue={newValue}, lastKnown={_lastKnownValue}, equal={Equals(newValue, _lastKnownValue)}");
            Logger.LogInformation($"newValue={newValue}, lastKnown={_lastKnownValue}, equal={Equals(newValue, _lastKnownValue)}");
            if (!Equals(newValue, _lastKnownValue))
            {
                _lastKnownValue = newValue;
                ValueUpdated?.Invoke(this, newValue);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowsStoragePropertyWatcher] Exception in Query_ContentsChanged: {ex}");
        }
    }

    public void Dispose()
    {
        if (_query != null)
        {
            _query.ContentsChanged -= Query_ContentsChanged;
            (_query as IDisposable)?.Dispose();
            _query = null;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
#endif