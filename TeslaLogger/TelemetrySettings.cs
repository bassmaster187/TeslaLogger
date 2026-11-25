using System;

namespace TeslaLogger
{
    internal static class TelemetrySettings
    {
        internal static string TelemetryServerURL => GetValue(
            nameof(TelemetryServerURL), ApplicationSettings.Default.TelemetryServerURL);

        internal static string TelemetryServerType => GetValue(
            nameof(TelemetryServerType), ApplicationSettings.Default.TelemetryServerType);

        internal static string TeslaHttpProxyURL => GetValue(
            nameof(TeslaHttpProxyURL), ApplicationSettings.Default.TeslaHttpProxyURL);

        internal static string TelemetryClientID => GetValue(
            nameof(TelemetryClientID), ApplicationSettings.Default.TelemetryClientID);

        internal static void LogOverrides()
        {
            foreach (string name in new[] { nameof(TelemetryServerURL), nameof(TelemetryServerType), nameof(TeslaHttpProxyURL), nameof(TelemetryClientID) })
            {
                string configured = (string)ApplicationSettings.Default[name];
                string value = GetValue(name, configured);
                string environmentValue = Environment.GetEnvironmentVariable(name);
                string defaultValue = Convert.ToString(ApplicationSettings.Default.Properties[name].DefaultValue);
                if (string.IsNullOrWhiteSpace(environmentValue) && string.Equals(value, defaultValue, StringComparison.Ordinal))
                    continue;

                string source = string.IsNullOrWhiteSpace(environmentValue) ? "configuration" : "environment";
                if (name.EndsWith("URL", StringComparison.Ordinal) && Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                {
                    // Never log URL credentials, query parameters or fragments.
                    value = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
                }
                else if (name.EndsWith("URL", StringComparison.Ordinal))
                {
                    value = "<invalid URL>";
                }
                Logfile.Log($"Configuration: {name}={value?.Replace("\r", "").Replace("\n", "")} (source: {source})");
            }
        }
        private static string GetValue(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
