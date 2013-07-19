using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ScrewTurn.Wiki;
using ScrewTurn.Wiki.PluginFramework;

namespace PageList
{

    /// <summary>
    /// Implements a formatter provider that counts download of files and attachments.
    /// </summary>
    public class PageList : IFormatterProviderV30
    {
        private IHostV30 _host;
        private string _config;
        private bool _enableLogging = true;
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly ComponentInformation Info = new ComponentInformation("PageList plugin", 
            "Michael Paulukonis", version, 
            "http://MichaelPaulukonis.com", 
            "http://localhost/release/pagelist-update.txt");

        // (\\s+.*?)?

        // private static readonly Regex tokenRegex = new Regex(@"{pagelist}",
        private static readonly Regex TokenRegex = new Regex(@"{pagelist(\s+.*?)?}", RegexOptions);

        private static readonly RegexOptions RegexOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        private static readonly List<string> FilterList = new List<string> { "namespace", "include", "exclude" };

        /// <summary>
        /// Specifies whether or not to execute Phase 1.
        /// </summary>
        public bool PerformPhase1
        {
            get { return false; }
        }

        /// <summary>
        /// Specifies whether or not to execute Phase 2.
        /// </summary>
        public bool PerformPhase2
        {
            get { return false; }
        }

        /// <summary>
        /// Specifies whether or not to execute Phase 3.
        /// </summary>
        public bool PerformPhase3
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the execution priority of the provider (0 lowest, 100 highest).
        /// </summary>
        public int ExecutionPriority
        {
            get { return 50; }
        }

        /// <summary>
        /// Performs a Formatting phase.
        /// </summary>
        /// <param name="raw">The raw content to Format.</param>
        /// <param name="context">The Context information.</param>
        /// <param name="phase">The Phase.</param>
        /// <returns>The Formatted content.</returns>
        public string Format(string raw, ContextInformation context, FormattingPhase phase)
        {

            // see also: d:/projects/screwturn/greenicicle whatever
            // Format() for how it parses out the param within the block
            // I'd prefer a key=value pair, but that can be done the same way, I suppose
            // just pass the grabbed value to a parser
            // loop over key=value pairs until no more found
            // assume rest is text

            // copied from unfuddle
            var buffer = new StringBuilder(raw);

            //  get all matches
            //  loop through matches
            //     build list from match object info
            //     replace original span with list

            // because we replace an original match with a value of a (probably) different length
            // we cannot cycle through all the matches
            // we can either retrieve a new set of matches
            // OR go through the matches in reverse.....

            var match = GetAllMatches(buffer);
            while (match.Success)
            {
                buffer.Remove(match.Index, match.Length);
                var parms = ((match.Groups.Count > 1) ? match.Groups[1].ToString() : string.Empty);
                buffer.Insert(match.Index, BuildResult(parms));
                match = GetAllMatches(buffer);
            }

            return buffer.ToString();

        }

        /// <summary>
        /// Builds the result.
        /// </summary>
        /// <param name="raw">unparsed string of (potential) parameters</param>
        /// <returns>The result.</returns>
        private string BuildResult(string raw)
        {

            // parse out the possible values inside of block.Value
            // if key = namespace, use namespace
            // if namespace is invalid, default to current [ie, ignore]
            // if namespace is blank, ignore

            var parms = ParseParameters(raw);

            var nspace = string.Empty;
            if (parms.ContainsKey("namespace"))
            {
                nspace = parms["namespace"];
            }
            else
            {
                // returns null for the root namespace, which ... doesn't really exist. dammit.
                var ns = Tools.DetectCurrentNamespaceInfo();
                if (ns != null) nspace = ns.Name;
            }

            // handle the root namespace, which is (annoyingly) null
            if (nspace == string.Empty || nspace == "root" || nspace == "<root>")
            {
                nspace = null;
            }

            var nsinfo = (nspace != null ? Pages.FindNamespace(nspace) : null);
            var buffer = new StringBuilder();
            buffer.Append("<p><strong>" + (nspace ?? "&lt;root&gt;") + "</strong></p>");
            buffer.Append("<ul>");
            foreach (var page in Pages.GetPages(nsinfo))
            {
                buffer.Append(@"<li><a href=""");
                UrlTools.BuildUrl(buffer, Tools.UrlEncode(page.FullName), Settings.PageExtension);
                buffer.Append(@""">");
                buffer.Append(Content.GetPageContent(page, true).Title);
                buffer.Append("</a></li>");
            }
            buffer.Append("</ul>");

            return buffer.ToString();
        }

        private Dictionary<string, string> ParseParameters(string parms)
        {

            var parameters = new Dictionary<string, string>();

            var pairs = Regex.Split(parms, @"\s", RegexOptions);

            foreach (var pair in pairs)
            {
                var kv = pair.Split('=');
                if (kv.Length == 2) // ignore unless it is an equal-separated-pair
                {
                    kv[0] = kv[0].ToLowerInvariant();
                    // ignore unknown parameters
                    if (FilterList.Contains(kv[0]))
                    {
                        parameters.Add(kv[0], kv[1]);
                    }
                }
            }

            return parameters;

        }

        /// <summary>
        /// Replaces a placeholder with its value.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="placeholder">The placeholder.</param>
        /// <param name="value">The value.</param>
        private static void ReplacePlaceholder(StringBuilder buffer, string placeholder, string value)
        {
            var index = -1;

            do
            {
                index = buffer.ToString().ToLowerInvariant().IndexOf(placeholder);
                if (index != -1)
                {
                    buffer.Remove(index, placeholder.Length);
                    buffer.Insert(index, value);
                }
            } while (index != -1);
        }

        /// <summary>
        /// Tries to get the value of an attribute.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="attribute">The name of the attribute.</param>
        /// <returns>The value of the attribute or <c>null</c> if no value is available.</returns>
        private static string TryGetAttribute(XmlNode node, string attribute)
        {
            Debug.Assert(node.Attributes != null, "node.Attributes != null");
            var attr = node.Attributes[attribute];
            if (attr != null) return attr.Value;
            return null;
        }


        private Match GetAllMatches(StringBuilder buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            var match = TokenRegex.Match(buffer.ToString());
            return match;
        }

        // TODO: return all matches in reverse order
        private List<Match> GetAllMatches2(StringBuilder buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            var match = TokenRegex.Matches(buffer.ToString());
            var l = new List<Match>();
            for (var i = match.Count - 1; i >= 0; i--)
            {
                l.Add(match[i]);
            }

            return l;

        } 

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        private void LogWarning(string message)
        {
            if (_enableLogging)
            {
                _host.LogEntry(message, LogEntryType.Warning, null, this);
            }
        }

        /// <summary>
        /// Prepares the title of an item for display (always during phase 3).
        /// </summary>
        /// <param name="title">The input title.</param>
        /// <param name="context">The context information.</param>
        /// <returns>The prepared title (no markup allowed).</returns>
        public string PrepareTitle(string title, ContextInformation context)
        {
            return title;
        }

        /// <summary>
        /// Initializes the Storage Provider.
        /// </summary>
        /// <param name="host">The Host of the Component.</param>
        /// <param name="config">The Configuration data, if any.</param>
        /// <remarks>If the configuration string is not valid, the methoud should throw a <see cref="InvalidConfigurationException"/>.</remarks>
        public void Init(IHostV30 host, string config)
        {
            this._host = host;
            this._config = config ?? string.Empty;

            if (this._config.ToLowerInvariant() == "nolog") _enableLogging = false;
        }

        /// <summary>
        /// Method invoked on shutdown.
        /// </summary>
        /// <remarks>This method might not be invoked in some cases.</remarks>
        public void Shutdown()
        {
            // Nothing to do
        }

        /// <summary>
        /// Gets the Information about the Provider.
        /// </summary>
        public ComponentInformation Information
        {
            get { return Info; }
        }

        /// <summary>
        /// Gets a brief summary of the configuration string format, in HTML. Returns <c>null</c> if no configuration is needed.
        /// </summary>
        public string ConfigHelpHtml
        {
            get
            {
                return string.Format("Version: {0}\n<a href='{1}'>UpdateURL</a>", Info.Version, Info.UpdateUrl);
            }
        }

    }

}