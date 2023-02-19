using System.Web;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace TeslaLogger
{
    public class TeslaAuth
    {
        readonly string UserAgent = "TLV1";
        readonly HttpClient client;
        readonly LoginInfo loginInfo;
        static readonly Random Random = new Random();

        public enum TeslaAccountRegion
        {
            Unknown,
            USA,
            China
        }

        public TeslaAuth() 
        {
            loginInfo = new LoginInfo
            {
                CodeVerifier = RandomString(86),
                State = RandomString(20)
            };
            client = CreateHttpClient(TeslaAccountRegion.USA);

        }
        public static string RandomString(int length)
        {
            // Technically this should include the characters '-', '.', '_', and '~'.  However let's
            // keep this simpler for now to avoid potential URL encoding issues.
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            lock (Random)
            {
                return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[Random.Next(s.Length)]).ToArray());
            }
        }

        HttpClient CreateHttpClient(TeslaAccountRegion region)
        {
            var ch = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false,
                UseCookies = true
            };

            var client = new HttpClient(ch)
            {
                BaseAddress = new Uri(GetBaseAddressForRegion(region)),
                DefaultRequestHeaders =
                {
                    ConnectionClose = false,
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                }
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

            return client;
        }

        static string GetBaseAddressForRegion(TeslaAccountRegion region)
        {
            switch (region)
            {
                case TeslaAccountRegion.Unknown:
                case TeslaAccountRegion.USA:
                    return "https://auth.tesla.com";

                case TeslaAccountRegion.China:
                    return "https://auth.tesla.cn";

                default:
                    throw new NotImplementedException("Fell threw switch in GetBaseAddressForRegion for " + region);
            }
        }

        public string GetLoginUrlForBrowser()
        {
            byte[] code_challenge_SHA256 = ComputeSHA256HashInBytes(loginInfo.CodeVerifier);
            loginInfo.CodeChallenge = Base64UrlEncode(code_challenge_SHA256);

            var b = new UriBuilder(client.BaseAddress + "/oauth2/v3/authorize") { Port = -1 };

            var q = HttpUtility.ParseQueryString(b.Query);
            q["client_id"] = "ownerapi";
            q["code_challenge"] = loginInfo.CodeChallenge;
            q["code_challenge_method"] = "S256";
            q["redirect_uri"] = "https://auth.tesla.com/void/callback";
            q["response_type"] = "code";
            q["scope"] = "openid email offline_access";
            q["state"] = loginInfo.State;
            //q["locale"] = "en-US";
            b.Query = q.ToString();
            return b.ToString();
        }

        public async Task<Tokens> GetTokenAfterLoginAsync(string redirectUrl, CancellationToken cancellationToken = default)
        {
            // URL is something like https://auth.tesla.com/void/callback?code=b6a6a44dea889eb08cd8afe5adc16353662cc5d82ba0c6044c95b13d6f…"
            var b = new UriBuilder(redirectUrl);
            var q = HttpUtility.ParseQueryString(b.Query);
            var code = q["code"];

            // As of March 21 2022, this returns a bearer token.  No need to call ExchangeAccessTokenForBearerToken
            var tokens = await ExchangeCodeForBearerTokenAsync(code, client, cancellationToken);
            return tokens;
            /*
            var accessAndRefreshTokens = await ExchangeAccessTokenForBearerTokenAsync(tokens.AccessToken, client, cancellationToken);
            return new Tokens
            {
                AccessToken = accessAndRefreshTokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                CreatedAt = accessAndRefreshTokens.CreatedAt,
                ExpiresIn = accessAndRefreshTokens.ExpiresIn
            };
            */
        }

        async Task<Tokens> ExchangeCodeForBearerTokenAsync(string code, HttpClient client, CancellationToken cancellationToken)
        {
            var body = new JObject
            {
                {"grant_type", "authorization_code"},
                {"client_id", "ownerapi"},
                {"code", code},
                {"code_verifier", loginInfo.CodeVerifier},
                {"redirect_uri", "https://auth.tesla.com/void/callback"},
                //{"locale", "en-US" },
            };

            var content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json");
            var result = await client.PostAsync(client.BaseAddress + "oauth2/v3/token", content, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                var failureDetails = result.Content.ReadAsStringAsync().Result;
                var message = string.IsNullOrEmpty(result.ReasonPhrase) ? result.StatusCode.ToString() : result.ReasonPhrase;
                message += " - " + failureDetails;
                throw new Exception(message);
            }

            string resultContent = await result.Content.ReadAsStringAsync();
            var response = JObject.Parse(resultContent);

            var tokens = new Tokens
            {
                AccessToken = response["access_token"].Value<string>(),
                RefreshToken = response["refresh_token"].Value<string>(),
                ExpiresIn = TimeSpan.FromSeconds(response["expires_in"].Value<long>()),
                TokenType = response["token_type"].Value<string>(),
                CreatedAt = DateTimeOffset.Now,
            };
            return tokens;
        }

        static byte[] ComputeSHA256HashInBytes(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] textAsBytes = GetBytes(text);  // was Encoding.Default.GetBytes(text) but that depends on the current machine's code page settings.
                var hash = sha256.ComputeHash(textAsBytes);
                return hash;
            }
        }

        public static byte[] GetBytes(String s)
        {
            // This is just a passthrough.  We want to make sure that behavior for characters with a
            // code point value >= 128 is passed through as-is, without depending on your current
            // machine's default ANSI code page or the exact behavior of ASCIIEncoding.  Some people
            // are using UTF-8 but that may vary the length of the code verifier, perhaps inappropriately.
            byte[] bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
                bytes[i] = (byte)s[i];
            return bytes;
        }

        public static string Base64UrlEncode(byte[] bytes)
        {
            String base64 = Convert.ToBase64String(bytes);
            String encoded = base64
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", String.Empty)
                .Trim();
            return encoded;
        }
    }

    internal class LoginInfo
    {
        public string CodeVerifier { get; set; }
        public string CodeChallenge { get; set; }
        public string State { get; set; }
        public Dictionary<string, string> FormFields { get; set; }
    }

    public class Tokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public TimeSpan ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }
}