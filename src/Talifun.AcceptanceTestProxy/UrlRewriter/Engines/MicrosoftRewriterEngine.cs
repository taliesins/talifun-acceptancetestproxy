using System;
using System.IO;
using System.Web;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Engines
{
	public class MicrosoftRewriterEngine : IRewriterEngine
	{
	    private readonly string _profilePath;
        private MicrosoftRuleSet _ruleSet;

        public MicrosoftRewriterEngine(string profilePath)
        {
            _profilePath = profilePath;

            var configurationFile = new FileInfo(Path.Combine(_profilePath, "urlrewrite.config"));
            _ruleSet = new MicrosoftRuleSet(configurationFile);
        }

        /// <summary>
        /// Refreshes the config.
        /// </summary>
        private void RefreshConfig()
        {
            _ruleSet.RefreshRules();
        }

        public void Init()
        {
            RefreshConfig();
        }

        public void RefreshRules()
        {
            RefreshConfig();
        }

        public void RunOutputRules(HttpContextBase context)
        {
            throw new NotImplementedException();
        }

        public Uri RunRules(HttpContextBase context)
        {
            var url = new Uri(context.Request.Url, context.Request.RawUrl);
            var rewritenUrl = _ruleSet.RunRules(context, url);

            return rewritenUrl;
        }
    }
}
