using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnitTestsTeslalogger
{
    /// <summary>
    /// Checks the admin web interface (TeslaLogger/www/admin) and the translation
    /// files (TeslaLogger/bin/language-*.txt).
    ///
    /// These are pure file tests: they need no database, no running Teslalogger and
    /// no browser, so they also work in CI. They cover the mistakes that were made
    /// while restyling the pages: a missing --skip-ssl in the restore, translation
    /// keys that exist only in English, a stylesheet that was changed without
    /// bumping its ?v= cache key, and CSS rules that recoloured the shared menu.
    /// </summary>
    [TestClass]
    public class UnitTestAdminPanel
    {
        /// <summary>the 15 languages of TeslaLogger/bin/language-*.txt</summary>
        private static readonly string[] Languages =
        {
            "cn", "da", "de", "en", "es", "fr", "it", "nl", "no", "pl", "pt", "ru", "sv", "ta", "tr"
        };

        /// <summary>tokens that are identical in both colour schemes, so they are defined only once</summary>
        private static readonly string[] SchemeIndependentTokens = { "--tl-radius", "--tl-gap" };

        /// <summary>keys whose value contains "=" and is therefore cut off by the parser of language.php</summary>
        private static readonly string[] KnownKeysWithEqualsSign = { "BA_NotFound" };

        /// <summary>files a user may create himself, referenced without being in the repository</summary>
        private static readonly string[] OptionalUserFiles = { "my_dashboard.css" };

        private static string AdminDir => Path.Combine(RepoRoot(), "TeslaLogger", "www", "admin");
        private static string BinDir => Path.Combine(RepoRoot(), "TeslaLogger", "bin");

        private static string RepoRoot()
        {
            // works from bin\Debug (legacy) and bin\Debug\net8.0 (net8) output directories
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "TeslaLogger", "www", "admin")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }

            throw new DirectoryNotFoundException("TeslaLogger/www/admin not found starting from " + AppContext.BaseDirectory);
        }

        private static string ReadAllText(string path)
        {
            // File.ReadAllText removes the byte order mark, which would otherwise
            // hide in front of the first key of a language file.
            return File.ReadAllText(path);
        }

        // ------------------------------------------------------------------------
        // translation files
        // ------------------------------------------------------------------------

        [TestMethod]
        public void LanguageFiles_ExistAndKeepUtf8BomAndCrlf()
        {
            // language.php reads these files line by line and the app writes them
            // back, so the encoding must stay UTF-8 with BOM and CRLF. A whole file
            // rewrite that drops one of both shows up as a diff of the complete file.
            foreach (string language in Languages)
            {
                string path = Path.Combine(BinDir, "language-" + language + ".txt");
                Assert.IsTrue(File.Exists(path), "missing language file: " + path);

                byte[] bytes = File.ReadAllBytes(path);
                Assert.IsTrue(bytes.Length > 3, "empty language file: " + language);
                Assert.IsTrue(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    language + ": file must start with a UTF-8 byte order mark");

                string text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                for (int i = text.IndexOf('\n'); i >= 0; i = text.IndexOf('\n', i + 1))
                {
                    if (i == 0 || text[i - 1] != '\r')
                    {
                        Assert.Fail(language + ": line " + (1 + text.Substring(0, i).Count(c => c == '\n'))
                            + " ends with LF instead of CRLF");
                    }
                }
            }
        }

        [TestMethod]
        public void RestorePages_EveryKeyIsTranslatedInEveryLanguage()
        {
            // t("...") falls back to English for keys that are missing and for empty
            // values, so a missing translation is easy to overlook. Check every key
            // the restore pages use in all 15 files.
            List<string> keys = GetTranslatedKeys("restore.php", "restore_upload.php");
            Assert.IsTrue(keys.Count > 20, "expected the restore pages to use more keys, found: " + keys.Count);

            foreach (string language in Languages)
            {
                Dictionary<string, string> translations = ReadLanguageFile(language);

                foreach (string key in keys)
                {
                    Assert.IsTrue(translations.ContainsKey(key),
                        language + ": key is missing in language-" + language + ".txt: " + key);
                    Assert.IsTrue(translations[key].Trim().Length > 0,
                        language + ": key has an empty value, the page would show English: " + key);
                }
            }
        }

        [TestMethod]
        public void LanguageFiles_ValuesDoNotAddNewEqualsSigns()
        {
            // language.php splits a line with explode("=") and uses only $a[1], so a
            // second "=" inside a value cuts the text off. BA_NotFound is the one
            // existing key with that problem; nobody must add more of them.
            List<string> offenders = new List<string>();

            foreach (string language in Languages)
            {
                foreach (KeyValuePair<string, string> entry in ReadLanguageFile(language))
                {
                    if (entry.Value.Contains("="))
                        offenders.Add(entry.Key);
                }
            }

            List<string> unknown = offenders.Distinct().Except(KnownKeysWithEqualsSign).ToList();
            Assert.AreEqual(0, unknown.Count,
                "keys whose value contains \"=\", which is truncated by t(): " + string.Join(", ", unknown)
                + ". Write the text without \"=\" or fix the parser in language.php");
        }

        /// <summary>all t("...") keys of the given pages in the admin folder</summary>
        private static List<string> GetTranslatedKeys(params string[] pages)
        {
            List<string> keys = new List<string>();

            foreach (string page in pages)
            {
                string php = ReadAllText(Path.Combine(AdminDir, page));
                foreach (Match m in Regex.Matches(php, "t\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
                    keys.Add(m.Groups[1].Value.Replace("\\\"", "\""));
            }

            return keys.Distinct().ToList();
        }

        /// <summary>
        /// the language file as key/value pairs, using the same splitting rules as
        /// t() in language.php: the first "=" separates key and value, lines without
        /// "=" are comments. Empty values are returned (t() ignores them and falls
        /// back to English), quoted values are unquoted like in the web interface.
        /// </summary>
        private static Dictionary<string, string> ReadLanguageFile(string language)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>();

            foreach (string line in File.ReadAllLines(Path.Combine(BinDir, "language-" + language + ".txt")))
            {
                int separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                string key = line.Substring(0, separator);
                string value = line.Substring(separator + 1);

                if (value.Length > 1 && value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Substring(1, value.Length - 2);
                value = value.Replace("\"_QQ_\"", "\"");

                entries[key] = value;
            }

            return entries;
        }

        // ------------------------------------------------------------------------
        // stylesheet
        // ------------------------------------------------------------------------

        [TestMethod]
        public void Stylesheet_CacheKeyIsTheSameOnEveryPage()
        {
            // The pages load static/teslalogger_ui.css?v=N. Browsers keep the old file
            // when N is not raised, so a changed stylesheet must be bumped everywhere
            // at once - a single page with an old number serves a stale design.
            Dictionary<string, List<string>> pagesByVersion = new Dictionary<string, List<string>>();

            foreach (string file in Directory.GetFiles(AdminDir, "*.php"))
            {
                foreach (Match m in Regex.Matches(ReadAllText(file), "teslalogger_ui\\.css\\?v=(\\d+)"))
                {
                    string version = m.Groups[1].Value;
                    if (!pagesByVersion.ContainsKey(version))
                        pagesByVersion[version] = new List<string>();
                    pagesByVersion[version].Add(Path.GetFileName(file));
                }
            }

            Assert.IsTrue(pagesByVersion.Count > 0, "no page loads teslalogger_ui.css");
            Assert.AreEqual(1, pagesByVersion.Count,
                "teslalogger_ui.css is loaded with different cache keys: "
                + string.Join(", ", pagesByVersion.Select(v => "v" + v.Key + " (" + v.Value.Count + " pages: " + string.Join(" ", v.Value) + ")")));
            Assert.IsTrue(pagesByVersion.Values.First().Count >= 20,
                "expected at least 20 pages to load the stylesheet, found " + pagesByVersion.Values.First().Count);
        }

        [TestMethod]
        public void RestorePages_UseOnlyClassesDefinedInTheStylesheet()
        {
            string css = ReadAllText(Path.Combine(AdminDir, "static", "teslalogger_ui.css"));

            foreach (string className in GetDesignClasses("restore.php", "restore_upload.php"))
                Assert.IsTrue(Regex.IsMatch(css, "\\." + Regex.Escape(className) + "(?![-\\w])"),
                    className + " is used by the restore pages but not defined in teslalogger_ui.css");
        }

        /// <summary>every tl-* class name used by the given pages, including the ones built in echo()</summary>
        private static List<string> GetDesignClasses(params string[] pages)
        {
            List<string> classes = new List<string>();

            foreach (string page in pages)
            {
                string php = ReadAllText(Path.Combine(AdminDir, page));
                foreach (Match m in Regex.Matches(php, "(?<![-\\w])(tl-[a-z]+(?:-[a-z]+)*)"))
                    classes.Add(m.Groups[1].Value);
            }

            return classes.Distinct().ToList();
        }

        [TestMethod]
        public void Stylesheet_ColourTokensAreDefinedInBothColourSchemes()
        {
            // The light values are the default, the dark block replaces them. A token
            // that is only defined in one of both keeps the light colour in the other
            // scheme, which is unreadable on a dark card.
            string css = ReadAllText(Path.Combine(AdminDir, "static", "teslalogger_ui.css"));

            Match light = Regex.Match(css, ":root\\s*\\{(.*?)\\}", RegexOptions.Singleline);
            Assert.IsTrue(light.Success, "no :root block with the design tokens found");

            Match dark = Regex.Match(css, "@media \\(prefers-color-scheme: dark\\)\\s*\\{\\s*:root\\s*\\{(.*?)\\}", RegexOptions.Singleline);
            Assert.IsTrue(dark.Success, "no dark :root block found");

            List<string> lightTokens = GetTokenNames(light.Groups[1].Value);
            List<string> darkTokens = GetTokenNames(dark.Groups[1].Value);

            foreach (Match m in Regex.Matches(css, "var\\((--tl-[a-z-]+)\\)"))
                Assert.IsTrue(lightTokens.Contains(m.Groups[1].Value),
                    m.Groups[1].Value + " is used but never defined in :root");

            foreach (string token in darkTokens)
                Assert.IsTrue(lightTokens.Contains(token),
                    token + " is defined for the dark scheme only, the light scheme has no value for it");

            foreach (string token in lightTokens)
            {
                if (SchemeIndependentTokens.Contains(token))
                    continue;

                Assert.IsTrue(darkTokens.Contains(token),
                    token + " has no dark value. Add it to the prefers-color-scheme: dark block, "
                    + "or to SchemeIndependentTokens if it is the same in both schemes");
            }
        }

        private static List<string> GetTokenNames(string block)
        {
            return Regex.Matches(block, "(--tl-[a-z-]+)\\s*:").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        }

        [TestMethod]
        public void Stylesheet_DesignLinkColoursDoNotApplyToTheMenu()
        {
            // menu.php is a shared component and some pages include it inside
            // .tl-page. Without the :not(.site-navigation a) the menu links would use
            // the green link colour of the design instead of the menu colours.
            List<string> rules = GetRulesSetting(body => body.Contains("var(--tl-link"), ".tl-page a");

            Assert.IsTrue(rules.Count >= 2, "expected the normal and the hover rule of .tl-page a");
            foreach (string rule in rules)
                Assert.IsTrue(rule.Contains(":not(.site-navigation a)"),
                    "this rule would also colour the menu links: " + rule);
        }

        [TestMethod]
        public void Stylesheet_InputColoursDoNotApplyToButtons()
        {
            // The dark scheme gives the input fields a dark background. That rule must
            // not include the submit, button and reset inputs, they keep the green
            // button style of the theme (restore.php was grey because of this).
            List<string> rules = GetRulesSetting(body => body.Contains("background-color: var(--tl-card)"), ".tl-page input");

            Assert.IsTrue(rules.Count >= 1, "no rule found that gives the input fields a card background");
            foreach (string rule in rules)
            {
                Assert.IsTrue(rule.Contains(":not([type=\"submit\"])"), "the submit input must be excluded: " + rule);
                Assert.IsTrue(rule.Contains(":not([type=\"button\"])"), "the button input must be excluded: " + rule);
                Assert.IsTrue(rule.Contains(":not([type=\"reset\"])"), "the reset input must be excluded: " + rule);
            }
        }

        /// <summary>all "selector { body }" rules whose selector contains one of the given selectors</summary>
        private static List<string> GetRulesSetting(Func<string, bool> bodyCondition, params string[] selectors)
        {
            string css = ReadAllText(Path.Combine(AdminDir, "static", "teslalogger_ui.css"));
            List<string> matching = new List<string>();

            foreach (Match rule in Regex.Matches(css, "([^{}]+)\\{([^{}]*)\\}"))
            {
                string selector = rule.Groups[1].Value.Trim();
                string body = rule.Groups[2].Value;

                if (!selectors.Any(s => selector.Contains(s)))
                    continue;
                if (!bodyCondition(body))
                    continue;

                matching.Add(selector);
            }

            return matching;
        }

        // ------------------------------------------------------------------------
        // page markup
        // ------------------------------------------------------------------------

        [TestMethod]
        public void Pages_IncludeTheMenuBeforeThePageContainer()
        {
            // The header of menu.php must be outside .tl-page: inside it the links are
            // recoloured by the design and the header is limited to the content width.
            foreach (string file in Directory.GetFiles(AdminDir, "*.php"))
            {
                string name = Path.GetFileName(file);
                if (name == "menu.php")
                    continue;

                string php = ReadAllText(file);
                int menu = php.IndexOf("menu.php", StringComparison.Ordinal);
                int page = php.IndexOf("class=\"tl-page\"", StringComparison.Ordinal);

                if (menu < 0 || page < 0)
                    continue;

                Assert.IsTrue(menu < page,
                    name + ": include \"menu.php\" must come before <div class=\"tl-page\">");
            }
        }

        [TestMethod]
        public void Pages_ReferenceOnlyExistingScriptAndStylesheetFiles()
        {
            // admin/jquery/jquery-migrate-1.4.1.min.js was referenced on six pages
            // although the file lives in static/jquery/, and the 404 was only visible
            // in the browser console.
            List<string> missing = new List<string>();

            foreach (string file in Directory.GetFiles(AdminDir, "*.php"))
            {
                string name = Path.GetFileName(file);

                foreach (Match m in Regex.Matches(ReadAllText(file), "(?:src|href)=\"([^\"]+\\.(?:js|css))(?:\\?[^\"]*)?\""))
                {
                    string reference = m.Groups[1].Value;
                    if (reference.StartsWith("http") || reference.StartsWith("//") || reference.Contains("<?") || reference.Contains("$"))
                        continue;
                    if (OptionalUserFiles.Contains(Path.GetFileName(reference)))
                        continue;   // dashboard.md: the user may create this file himself

                    string path = Path.Combine(AdminDir, reference.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(path))
                        missing.Add(name + " -> " + reference);
                }
            }

            Assert.AreEqual(0, missing.Count, "missing files: " + string.Join(", ", missing));
        }

        // ------------------------------------------------------------------------
        // restore_upload.php: the bug that made the restore fail silently
        // ------------------------------------------------------------------------

        [TestMethod]
        public void RestoreUpload_CallsMysqlWithSkipSslAndCapturesTheErrors()
        {
            // The webserver image (php:8.5-apache-trixie) ships mariadb-client 11.x,
            // which requires TLS by default while the database image (mariadb:10.4.7)
            // offers none: without --skip-ssl the restore stops with
            // "ERROR 2026 (HY000): TLS/SSL error". exec() returns only stdout, so the
            // errors are lost without 2>&1.
            string php = ReadAllText(Path.Combine(AdminDir, "restore_upload.php"));

            List<string> commands = Regex.Matches(php, "exec\\(\"(/usr/bin/mysql[^\"]*)\"")
                .Cast<Match>().Select(m => m.Groups[1].Value).ToList();

            Assert.AreEqual(2, commands.Count, "expected the Docker and the non Docker mysql call");

            foreach (string command in commands)
                Assert.IsTrue(command.Contains("2>&1"), "mysql errors are not captured: " + command);

            Assert.IsTrue(commands.Any(c => c.Contains("--skip-ssl")),
                "the mysql call inside the Docker branch needs --skip-ssl");
        }

        [TestMethod]
        public void RestoreUpload_FailsLoudlyWhenMysqlReturnsAnError()
        {
            // The old code logged "Restore completed successfully" even when mysql
            // returned 1, because the return value was never checked.
            string php = ReadAllText(Path.Combine(AdminDir, "restore_upload.php"));

            Assert.IsTrue(Regex.IsMatch(php, "if\\s*\\(\\s*\\$return_var\\s*!==?\\s*0\\s*\\)"),
                "the return value of mysql is not checked");
            Assert.IsTrue(php.Contains("restore_hint("),
                "a failed restore must show a hint for the mysql error code");
        }

        [TestMethod]
        public void RestoreUpload_RestartButtonGoesToTheStartPage()
        {
            // The result page cannot reload itself: that would start the restore again.
            // It restarts Teslalogger through the normal menu flow and then switches to
            // index.php.
            string php = ReadAllText(Path.Combine(AdminDir, "restore_upload.php"));

            int start = php.IndexOf("confirmAction(", StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, "the restart button does not use confirmAction() of menu.php");

            // the call is built from several echo parts, so look at the text behind it
            // instead of trying to match the closing bracket
            string parameters = php.Substring(start, Math.Min(600, php.Length - start));
            Assert.IsTrue(parameters.Contains("'restartlogger.php'"), "the button must call restartlogger.php: " + parameters);
            Assert.IsTrue(parameters.Contains("'index.php'"), "the button must switch to index.php afterwards: " + parameters);
            Assert.IsTrue(parameters.Contains(", false,"), "the page must not reload itself: " + parameters);
        }
    }
}
