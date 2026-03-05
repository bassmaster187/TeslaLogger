using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    // Lightweight adapter that exposes a small subset of WebClient's synchronous and asynchronous API
    // backed by HttpClient. This lets us replace obsolete WebClient usage with a compatible wrapper
    // without touching every call site's control flow.
    public class ModernWebClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public WebHeaderCollection Headers { get; } = new WebHeaderCollection();

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public ModernWebClient()
        {
            _httpClient = new HttpClient();
        }

        private void ApplyHeaders(HttpRequestMessage req)
        {
            if (Headers == null) return;
            foreach (string key in Headers.AllKeys)
            {
                try
                {
                    // Some headers must be added to DefaultRequestHeaders, some to request directly.
                    if (!req.Headers.TryAddWithoutValidation(key, Headers[key]))
                    {
                        if (req.Content == null) req.Content = new StringContent(string.Empty);
                        req.Content.Headers.TryAddWithoutValidation(key, Headers[key]);
                    }
                }
                catch
                {
                    // best-effort: ignore invalid headers
                }
            }
        }

        public string DownloadString(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyHeaders(req);
            var resp = _httpClient.SendAsync(req).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            var s = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return s;
        }

        public Task<string> DownloadStringTaskAsync(Uri uri)
        {
            return DownloadStringTaskAsync(uri.ToString());
        }

        public async Task<string> DownloadStringTaskAsync(string url)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyHeaders(req);
            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public void DownloadFile(string url, string filename)
        {
            var stream = _httpClient.GetStreamAsync(url).GetAwaiter().GetResult();
            using (var fs = File.Create(filename))
            {
                stream.CopyTo(fs);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        // Provide a virtual GetWebRequest so legacy overrides that expect WebClient's
        // extensibility point can still compile.
        protected virtual WebRequest GetWebRequest(Uri uri)
        {
            try
            {
                return WebRequest.Create(uri);
            }
            catch
            {
                return null;
            }
        }
    }
}
