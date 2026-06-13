// Auto-generated fallback - used on Windows where bash is not available
// On Linux/macOS, the build generates a version with git branch and date
using System.Reflection;

namespace TeslaLogger
{
    internal static class BuildInfo
    {
        public const string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public const string GitBranch = "unknown";
        public const string GitCommit = "unknown";
        public const string BuildDate = "unknown";
        public const string FullVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}