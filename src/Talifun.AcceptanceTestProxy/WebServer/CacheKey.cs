namespace Talifun.AcceptanceTestProxy.WebServer
{
    public class CacheKey
    {
        public string AbsoluteUri { get; set; }
        public string UserAgent { get; set; }
        public string Profile { get; set; }

        public CacheKey(string requestUri, string userAgent, string profile)
        {
            AbsoluteUri = requestUri;
            UserAgent = userAgent;
            Profile = profile;
        }

        public override bool Equals(object obj)
        {
            var key = obj as CacheKey;
            if (key != null)
            {
            	return (key.AbsoluteUri == AbsoluteUri && key.UserAgent == UserAgent && key.Profile == Profile);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return AbsoluteUri + "|" + UserAgent + "|" + Profile;
        }
    }
}
