using System;
using System.Configuration;
using System.IO;
using System.Net;
using Talifun.AcceptanceTestProxy.UrlRewriter.Engines;
using Talifun.AcceptanceTestProxy.UrlRewriter.Wrappers;
using log4net;

namespace Talifun.AcceptanceTestProxy.Profiles
{
    public class ProfileManager : IProfileManager
    {
        public const string DefaultProfileName = "default";
        private static readonly object ProfileCreatorLock = new object();
		private static readonly ILog Logger = LogManager.GetLogger(typeof(ProfileManager));
		private readonly string _profileRootPath;
		private readonly ProfileCache _profileCache;

        public ProfileManager(ProfileCache profileCache)
            : this(ProfileRootPath, profileCache)
        {
        }

        public ProfileManager(string profileRootPath, ProfileCache profileCache)
		{
			_profileRootPath = profileRootPath;
			_profileCache = profileCache;
		}

        private static string ProfileRootPath
        {
            get
            {
                var profilePath = "Profiles\\";
                if (ConfigurationManager.AppSettings["ProfilePath"] != null)
                {
                    profilePath = ConfigurationManager.AppSettings["ProfilePath"];
                }

                return profilePath;
            }
        }

		public Uri HandleRequest(string profileName, HttpWebRequest request)
		{
            if (string.IsNullOrEmpty(profileName))
            {
                profileName = DefaultProfileName;
            }
			var profile = _profileCache.GetProfile(profileName);
			if (profile == null)
			{
			    var profilepath = Path.Combine(_profileRootPath, profileName + "\\");

                lock (ProfileCreatorLock)
                {
                    profile = _profileCache.GetProfile(profileName);
                    if (profile == null)
                    {
                        profile = new Profile();
                        if (Directory.Exists(profilepath))
                        {
                            var profileRewriterEngine = new MicrosoftRewriterEngine(profilepath);
                            profileRewriterEngine.Init();
                            profile.RewriterEngine = profileRewriterEngine;

                            Logger.InfoFormat("Profile loaded for {0}", profileName);
                        }
                        else
                        {
                            Logger.InfoFormat("No profile for {0}", profileName);
                        }

                        _profileCache.AddProfile(profileName, profile);
                    }
                }
			}

            if (profile.RewriterEngine == null)
            {
                return null;
            }

		    var httpContext = new HttpContext(request);

			var rewrittenUrl = profile.RewriterEngine.RunRules(httpContext);

		    return rewrittenUrl;
		}
	}
}
