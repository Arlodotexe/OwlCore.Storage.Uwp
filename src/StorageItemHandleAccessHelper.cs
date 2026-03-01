#if WINDOWS_UWP
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace OwlCore.Storage.Uwp;

/// <summary>
/// Provides helper methods for accessing native file handles from <see cref="IStorageItem"/> instances
/// using the <c>IStorageItemHandleAccess</c> COM interface.
/// </summary>
/// <remarks>
/// <para>
/// The Windows.Storage property system caches date properties and does not support writing them directly.
/// This helper uses the <c>IStorageItemHandleAccess</c> COM interface to obtain a Win32 file handle,
/// which can then be used with <c>GetFileTime</c>/<c>SetFileTime</c> to read/modify timestamps.
/// </para>
/// <para>
/// This mechanism respects UWP sandbox permissions - if you have access to the <see cref="IStorageItem"/>,
/// you can obtain a handle to it. Works across all UWP device families (Desktop, Xbox, IoT, HoloLens).
/// </para>
/// </remarks>
internal static class StorageItemHandleAccessHelper
{
    private static readonly Guid IID_IStorageItemHandleAccess = new("5CA296B2-2C25-4D22-B785-B885C8201E6A");

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [Flags]
    private enum HANDLE_ACCESS_OPTIONS : uint
    {
        HAO_NONE = 0,
        HAO_READ_ATTRIBUTES = 0x80,
        HAO_READ = 0x120089,
        HAO_WRITE = 0x120116,
        HAO_DELETE = 0x10000,
    }

    [Flags]
    private enum HANDLE_SHARING_OPTIONS : uint
    {
        HSO_NONE = 0,
        HSO_SHARE_READ = 0x1,
        HSO_SHARE_WRITE = 0x2,
        HSO_SHARE_DELETE = 0x4,
    }

    [Flags]
    private enum HANDLE_OPTIONS : uint
    {
        HO_NONE = 0,
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateDelegate(
        IntPtr thisPtr,
        HANDLE_ACCESS_OPTIONS accessOptions,
        HANDLE_SHARING_OPTIONS sharingOptions,
        HANDLE_OPTIONS options,
        IntPtr oplockBreakingHandler,
        out IntPtr interopHandle);

    [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
    private static extern bool SetFileTime(
        IntPtr hFile,
        IntPtr lpCreationTime,
        IntPtr lpLastAccessTime,
        IntPtr lpLastWriteTime);

    [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
    private static extern bool GetFileTime(
        IntPtr hFile,
        out FILETIME lpCreationTime,
        out FILETIME lpLastAccessTime,
        out FILETIME lpLastWriteTime);

    [DllImport("api-ms-win-core-handle-l1-1-0.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private static readonly DateTimeOffset FileTimeEpoch = new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets a file handle for a storage item using the IStorageItemHandleAccess COM interface.
    /// </summary>
    private static IntPtr GetHandle(IStorageItem item, HANDLE_ACCESS_OPTIONS accessOptions, out IntPtr pUnk, out IntPtr pInterface)
    {
        pUnk = Marshal.GetIUnknownForObject(item);
        
        Guid iid = IID_IStorageItemHandleAccess;
        int hr = Marshal.QueryInterface(pUnk, ref iid, out pInterface);

        if (hr != 0)
        {
            Marshal.Release(pUnk);
            throw new InvalidOperationException($"Failed to obtain IStorageItemHandleAccess interface. HRESULT: 0x{hr:X8}");
        }

        IntPtr vtable = Marshal.ReadIntPtr(pInterface);
        IntPtr createPtr = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);
        var createFunc = Marshal.GetDelegateForFunctionPointer<CreateDelegate>(createPtr);

        hr = createFunc(
            pInterface,
            accessOptions,
            HANDLE_SHARING_OPTIONS.HSO_SHARE_READ | HANDLE_SHARING_OPTIONS.HSO_SHARE_WRITE,
            HANDLE_OPTIONS.HO_NONE,
            IntPtr.Zero,
            out IntPtr handle);

        if (hr != 0)
        {
            Marshal.Release(pInterface);
            Marshal.Release(pUnk);
            throw new InvalidOperationException($"Failed to create file handle. HRESULT: 0x{hr:X8}");
        }

        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            Marshal.Release(pInterface);
            Marshal.Release(pUnk);
            throw new InvalidOperationException("Invalid file handle returned.");
        }

        return handle;
    }

    /// <summary>
    /// Gets the timestamps from a storage item using Win32 GetFileTime for fresh (non-cached) values.
    /// </summary>
    /// <param name="item">The storage item to read from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <returns>A tuple containing the creation, last access, and last write times.</returns>
    public static Task<(DateTimeOffset? CreatedAt, DateTimeOffset? LastAccessedAt, DateTimeOffset? LastModifiedAt)> GetFileTimesAsync(
        IStorageItem item,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IntPtr pUnk = IntPtr.Zero;
        IntPtr pInterface = IntPtr.Zero;

        try
        {
            var handle = GetHandle(item, HANDLE_ACCESS_OPTIONS.HAO_READ_ATTRIBUTES, out pUnk, out pInterface);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool success = GetFileTime(handle, out var ftCreated, out var ftAccessed, out var ftModified);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"GetFileTime failed with Win32 error code: {error}");
                }

                var created = FileTimeToDateTimeOffset(ftCreated);
                var accessed = FileTimeToDateTimeOffset(ftAccessed);
                var modified = FileTimeToDateTimeOffset(ftModified);

                return Task.FromResult((
                    ToNullableIfSentinel(created),
                    ToNullableIfSentinel(accessed),
                    ToNullableIfSentinel(modified)
                ));
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        finally
        {
            if (pInterface != IntPtr.Zero)
                Marshal.Release(pInterface);
            if (pUnk != IntPtr.Zero)
                Marshal.Release(pUnk);
        }
    }

    /// <summary>
    /// Gets the creation timestamp from a storage item.
    /// </summary>
    public static async Task<DateTimeOffset?> GetCreatedAtAsync(IStorageItem item, CancellationToken cancellationToken = default)
    {
        var (createdAt, _, _) = await GetFileTimesAsync(item, cancellationToken);
        return createdAt;
    }

    /// <summary>
    /// Gets the last accessed timestamp from a storage item.
    /// </summary>
    public static async Task<DateTimeOffset?> GetLastAccessedAtAsync(IStorageItem item, CancellationToken cancellationToken = default)
    {
        var (_, lastAccessedAt, _) = await GetFileTimesAsync(item, cancellationToken);
        return lastAccessedAt;
    }

    /// <summary>
    /// Gets the last modified timestamp from a storage item.
    /// </summary>
    public static async Task<DateTimeOffset?> GetLastModifiedAtAsync(IStorageItem item, CancellationToken cancellationToken = default)
    {
        var (_, _, lastModifiedAt) = await GetFileTimesAsync(item, cancellationToken);
        return lastModifiedAt;
    }

    /// <summary>
    /// Sets the timestamps on a storage item.
    /// </summary>
    public static Task SetFileTimesAsync(
        IStorageItem item,
        DateTimeOffset? createdAt,
        DateTimeOffset? lastModifiedAt,
        DateTimeOffset? lastAccessedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IntPtr pUnk = IntPtr.Zero;
        IntPtr pInterface = IntPtr.Zero;
        IntPtr pCreated = IntPtr.Zero;
        IntPtr pAccessed = IntPtr.Zero;
        IntPtr pModified = IntPtr.Zero;

        try
        {
            var handle = GetHandle(item, HANDLE_ACCESS_OPTIONS.HAO_WRITE, out pUnk, out pInterface);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (createdAt.HasValue)
                {
                    var ftCreated = DateTimeOffsetToFileTime(createdAt.Value);
                    pCreated = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
                    Marshal.StructureToPtr(ftCreated, pCreated, false);
                }

                if (lastAccessedAt.HasValue)
                {
                    var ftAccessed = DateTimeOffsetToFileTime(lastAccessedAt.Value);
                    pAccessed = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
                    Marshal.StructureToPtr(ftAccessed, pAccessed, false);
                }

                if (lastModifiedAt.HasValue)
                {
                    var ftModified = DateTimeOffsetToFileTime(lastModifiedAt.Value);
                    pModified = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
                    Marshal.StructureToPtr(ftModified, pModified, false);
                }

                bool success = SetFileTime(handle, pCreated, pAccessed, pModified);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"SetFileTime failed with Win32 error code: {error}");
                }
            }
            finally
            {
                CloseHandle(handle);

                if (pCreated != IntPtr.Zero) Marshal.FreeHGlobal(pCreated);
                if (pAccessed != IntPtr.Zero) Marshal.FreeHGlobal(pAccessed);
                if (pModified != IntPtr.Zero) Marshal.FreeHGlobal(pModified);
            }
        }
        finally
        {
            if (pInterface != IntPtr.Zero)
                Marshal.Release(pInterface);
            if (pUnk != IntPtr.Zero)
                Marshal.Release(pUnk);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the creation timestamp on a storage item.
    /// </summary>
    public static Task SetCreatedAtAsync(IStorageItem item, DateTimeOffset createdAt, CancellationToken cancellationToken = default)
        => SetFileTimesAsync(item, createdAt, null, null, cancellationToken);

    /// <summary>
    /// Sets the last modified timestamp on a storage item.
    /// </summary>
    public static Task SetLastModifiedAtAsync(IStorageItem item, DateTimeOffset lastModifiedAt, CancellationToken cancellationToken = default)
        => SetFileTimesAsync(item, null, lastModifiedAt, null, cancellationToken);

    /// <summary>
    /// Sets the last accessed timestamp on a storage item.
    /// </summary>
    public static Task SetLastAccessedAtAsync(IStorageItem item, DateTimeOffset lastAccessedAt, CancellationToken cancellationToken = default)
        => SetFileTimesAsync(item, null, null, lastAccessedAt, cancellationToken);

    private static FILETIME DateTimeOffsetToFileTime(DateTimeOffset dateTime)
    {
        long fileTime = dateTime.UtcDateTime.ToFileTimeUtc();
        return new FILETIME
        {
            dwLowDateTime = (uint)(fileTime & 0xFFFFFFFF),
            dwHighDateTime = (uint)(fileTime >> 32)
        };
    }

    private static DateTimeOffset FileTimeToDateTimeOffset(FILETIME fileTime)
    {
        long ticks = ((long)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;
        return DateTimeOffset.FromFileTime(ticks);
    }

    /// <summary>
    /// Determines if a <see cref="DateTimeOffset"/> represents the sentinel value (file time epoch).
    /// </summary>
    public static bool IsSentinelValue(DateTimeOffset dateTime)
        => dateTime.UtcDateTime <= FileTimeEpoch.UtcDateTime;

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to a nullable value, returning <c>null</c> if it's the sentinel.
    /// </summary>
    public static DateTimeOffset? ToNullableIfSentinel(DateTimeOffset dateTime)
        => IsSentinelValue(dateTime) ? null : dateTime;
}
#endif
