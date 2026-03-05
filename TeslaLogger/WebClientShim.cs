using System.Text;

namespace TeslaLogger
{
    // Compatibility shim: existing code often constructs "new WebClient()".
    // Provide a lightweight WebClient class that derives from ModernWebClient so
    // earlier call sites keep working without changing many files at once.
    public class WebClient : ModernWebClient
    {
        public WebClient() : base() { }

        // Keep Encoding property similar to original WebClient
        public new System.Text.Encoding Encoding
        {
            get => base.Encoding;
            set => base.Encoding = value;
        }
    }
}
