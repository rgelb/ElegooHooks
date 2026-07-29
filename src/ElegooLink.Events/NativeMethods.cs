using System.Runtime.InteropServices;

namespace ElegooLink.Events;

internal static class NativeMethods
{
    internal const string LibraryName = "elegoo_link_bridge";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void EventCallback(nint utf8Json, nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int el_initialize(
        int logLevel,
        int enableConsoleLogging,
        EventCallback callback,
        nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint el_get_version();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint el_discover(int timeoutMs);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint el_connect(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string optionsJson);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint el_refresh_status(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string printerId);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint el_disconnect(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string printerId);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void el_cleanup();

    internal static string GetUtf8(nint pointer) =>
        Marshal.PtrToStringUTF8(pointer)
        ?? throw new ElegooLinkException("The native Elegoo Link bridge returned an empty response.");
}
