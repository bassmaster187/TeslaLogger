using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TeslaLogger
{
    /// <summary>
    /// Minimal client for Tesla's guest API. The endpoint is protected by
    /// Akamai Bot Manager, which rejects regular .NET HTTP clients. Requests
    /// are therefore sent through the bundled curl-impersonate native library
    /// with Chrome's TLS/HTTP fingerprint and HTTP/3.
    /// </summary>
    internal static class TeslaGuestCurl
    {
        const string ShimLibrary = "tesla_curl_shim";
        const string CurlLibrary = "tesla_curl_lib";

        const int CURLOPT_WRITEDATA = 10001;
        const int CURLOPT_URL = 10002;
        const int CURLOPT_POSTFIELDS = 10015;
        const int CURLOPT_POST = 47;
        const int CURLOPT_POSTFIELDSIZE = 60;
        const int CURLOPT_HTTPHEADER = 10023;
        const int CURLOPT_HTTP_VERSION = 84;
        const int CURLOPT_NOSIGNAL = 99;
        const int CURLOPT_TIMEOUT_MS = 155;
        const int CURLOPT_SSL_OPTIONS = 216;
        const int CURLOPT_ACCEPT_ENCODING = 10102;
        const int CURLOPT_WRITEFUNCTION = 20011;

        const int CURL_HTTP_VERSION_3 = 30;
        const int CURLSSLOPT_NATIVE_CA = 16;
        const int CURLINFO_RESPONSE_CODE = 0x200002;
        const long CURL_GLOBAL_ALL = 3;

        [DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern int shim_global_init(long flags);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern void curl_global_cleanup();

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr curl_easy_init();

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern void curl_easy_cleanup(IntPtr curl);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false)]
        static extern int curl_easy_impersonate(IntPtr curl, string target, int defaultHeaders);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern int curl_easy_perform(IntPtr curl);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr curl_easy_strerror(int code);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr curl_slist_append(IntPtr list, [MarshalAs(UnmanagedType.LPUTF8Str)] string data);

        [DllImport(CurlLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern void curl_slist_free_all(IntPtr list);

        [DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern int shim_easy_setopt_long(IntPtr curl, int option, long value);

        [DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern int shim_easy_setopt_ptr(IntPtr curl, int option, IntPtr value);

        [DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "shim_easy_setopt_ptr")]
        static extern int shim_easy_setopt_str(IntPtr curl, int option, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

        [DllImport(ShimLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern int shim_easy_getinfo_long(IntPtr curl, int info, out long value);

        delegate UIntPtr WriteCallback(IntPtr ptr, UIntPtr size, UIntPtr nmemb, IntPtr userdata);

        static readonly WriteCallback _writeCallback = WriteCallbackImpl;
        static readonly IntPtr _writeCallbackPtr = Marshal.GetFunctionPointerForDelegate(_writeCallback);
        static readonly object _initLock = new object();
        static bool _initialized;

        internal sealed class Response
        {
            public int StatusCode;
            public string Body;
            public string Error;
        }

        internal static Response Post(string url, string payload, string referer)
        {
            if (!EnsureInitialized(out string initError))
                return new Response { Error = initError };

            IntPtr handle = curl_easy_init();
            if (handle == IntPtr.Zero)
                return new Response { Error = "curl_easy_init failed" };

            IntPtr headers = IntPtr.Zero;
            IntPtr urlPtr = IntPtr.Zero;
            IntPtr payloadPtr = IntPtr.Zero;
            MemoryStream buffer = new MemoryStream();
            GCHandle bufferHandle = GCHandle.Alloc(buffer);

            try
            {
                Check(curl_easy_impersonate(handle, "chrome146", 1), "curl_easy_impersonate");

                headers = AppendHeader(headers, "content-type: application/json");
                headers = AppendHeader(headers, "accept: application/json, text/plain, */*");
                headers = AppendHeader(headers, "origin: https://www.tesla.com");
                if (!string.IsNullOrEmpty(referer))
                    headers = AppendHeader(headers, "referer: " + referer);
                headers = AppendHeader(headers, "sec-fetch-site: same-origin");
                headers = AppendHeader(headers, "sec-fetch-mode: cors");
                headers = AppendHeader(headers, "sec-fetch-dest: empty");

                // CURLOPT_POSTFIELDS does not copy the buffer, so keep both
                // strings alive as unmanaged memory until after perform.
                urlPtr = Marshal.StringToCoTaskMemUTF8(url);
                payloadPtr = Marshal.StringToCoTaskMemUTF8(payload);

                Check(shim_easy_setopt_ptr(handle, CURLOPT_URL, urlPtr), "CURLOPT_URL");
                Check(shim_easy_setopt_long(handle, CURLOPT_HTTP_VERSION, CURL_HTTP_VERSION_3), "CURLOPT_HTTP_VERSION");
                Check(shim_easy_setopt_long(handle, CURLOPT_POST, 1), "CURLOPT_POST");
                Check(shim_easy_setopt_ptr(handle, CURLOPT_POSTFIELDS, payloadPtr), "CURLOPT_POSTFIELDS");
                Check(shim_easy_setopt_long(handle, CURLOPT_POSTFIELDSIZE, Encoding.UTF8.GetByteCount(payload)), "CURLOPT_POSTFIELDSIZE");
                Check(shim_easy_setopt_ptr(handle, CURLOPT_HTTPHEADER, headers), "CURLOPT_HTTPHEADER");
                Check(shim_easy_setopt_ptr(handle, CURLOPT_WRITEFUNCTION, _writeCallbackPtr), "CURLOPT_WRITEFUNCTION");
                Check(shim_easy_setopt_ptr(handle, CURLOPT_WRITEDATA, GCHandle.ToIntPtr(bufferHandle)), "CURLOPT_WRITEDATA");
                Check(shim_easy_setopt_long(handle, CURLOPT_TIMEOUT_MS, 30000), "CURLOPT_TIMEOUT_MS");
                Check(shim_easy_setopt_long(handle, CURLOPT_NOSIGNAL, 1), "CURLOPT_NOSIGNAL");
                Check(shim_easy_setopt_str(handle, CURLOPT_ACCEPT_ENCODING, ""), "CURLOPT_ACCEPT_ENCODING");

                if (OperatingSystem.IsWindows())
                    Check(shim_easy_setopt_long(handle, CURLOPT_SSL_OPTIONS, CURLSSLOPT_NATIVE_CA), "CURLOPT_SSL_OPTIONS");

                int result = curl_easy_perform(handle);
                if (result != 0)
                {
                    string error = Marshal.PtrToStringAnsi(curl_easy_strerror(result));
                    return new Response { Error = $"curl error {result}: {error}" };
                }

                long statusCode = 0;
                shim_easy_getinfo_long(handle, CURLINFO_RESPONSE_CODE, out statusCode);

                return new Response
                {
                    StatusCode = (int)statusCode,
                    Body = Encoding.UTF8.GetString(buffer.ToArray())
                };
            }
            catch (Exception ex)
            {
                return new Response { Error = ex.ToString() };
            }
            finally
            {
                if (headers != IntPtr.Zero)
                    curl_slist_free_all(headers);

                if (payloadPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(payloadPtr);

                if (urlPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(urlPtr);

                bufferHandle.Free();
                curl_easy_cleanup(handle);
            }
        }

        static IntPtr AppendHeader(IntPtr list, string value)
        {
            IntPtr newList = curl_slist_append(list, value);
            if (newList == IntPtr.Zero)
                throw new InvalidOperationException("curl_slist_append failed for: " + value);
            return newList;
        }

        static void Check(int result, string option)
        {
            if (result != 0)
                throw new InvalidOperationException($"{option} failed with curl code {result}");
        }

        static UIntPtr WriteCallbackImpl(IntPtr ptr, UIntPtr size, UIntPtr nmemb, IntPtr userdata)
        {
            long total = (long)size * (long)nmemb;
            if (total <= 0)
                return UIntPtr.Zero;

            try
            {
                GCHandle handle = GCHandle.FromIntPtr(userdata);
                if (handle.Target is MemoryStream stream)
                {
                    byte[] buffer = new byte[total];
                    Marshal.Copy(ptr, buffer, 0, (int)total);
                    stream.Write(buffer, 0, (int)total);
                    return (UIntPtr)total;
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("TeslaGuestCurl write callback exception", ex);
            }

            return UIntPtr.Zero;
        }

        static bool EnsureInitialized(out string error)
        {
            error = null;

            if (_initialized)
                return true;

            lock (_initLock)
            {
                if (_initialized)
                    return true;

                try
                {
                    NativeLibrary.SetDllImportResolver(typeof(TeslaGuestCurl).Assembly, ResolveLibrary);
                    int result = shim_global_init(CURL_GLOBAL_ALL);
                    if (result != 0)
                    {
                        error = $"shim_global_init failed with curl code {result}";
                        return false;
                    }

                    _initialized = true;
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.ToString();
                    return false;
                }
            }
        }

        static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != ShimLibrary && libraryName != CurlLibrary)
                return IntPtr.Zero;

            string rid = GetRuntimeIdentifier();
            string fileName = GetNativeFileName(libraryName);

            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName)
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    return handle;
            }

            try
            {
                if (NativeLibrary.TryLoad(fileName, out IntPtr handle))
                    return handle;
            }
            catch (Exception)
            {
            }

            return IntPtr.Zero;
        }

        static string GetNativeFileName(string libraryName)
        {
            bool isWindows = OperatingSystem.IsWindows();
            bool isMacOS = OperatingSystem.IsMacOS();

            if (isWindows)
                return libraryName == CurlLibrary ? "libcurl-impersonate.dll" : "curl_shim.dll";

            if (isMacOS)
                return "libcurl_shim.dylib";

            return "libcurl_shim.so";
        }

        static string GetRuntimeIdentifier()
        {
            string arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

            if (OperatingSystem.IsWindows())
                return "win-" + arch;

            if (OperatingSystem.IsMacOS())
                return "osx-" + arch;

            return (IsMuslLinux() ? "linux-musl-" : "linux-") + arch;
        }

        static bool IsMuslLinux()
        {
            if (RuntimeInformation.RuntimeIdentifier.IndexOf("musl", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            try
            {
                return Directory.Exists("/lib")
                    && Directory.EnumerateFiles("/lib", "ld-musl-*.so.1").Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
