using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;

namespace Talifun.AcceptanceTestProxy.WebServer
{
    public class ProxyCache : IProxyCache
    {
        private readonly string _regionName;
        private readonly ObjectCache _cache;

        public ProxyCache()
            : this(null, MemoryCache.Default)
        {}

        public ProxyCache(string regionName, ObjectCache objectCache)
        {
            _regionName = regionName;
            _cache = objectCache;
        }

        public CacheEntry GetData(HttpWebRequest request, string profile)
        {
            var cacheKey = new CacheKey(request.RequestUri.AbsoluteUri, request.UserAgent, profile).ToString();

            DateTime? datetime = null;
            if (!CanCache(request.Headers, ref datetime))
            {
                _cache.Remove(cacheKey, _regionName);
                return null;
            }
            else
            {
                var entry = _cache.Get(cacheKey, _regionName) as CacheEntry;
                return entry;
            }
        }

        public CacheEntry MakeEntry(HttpWebRequest request, string profile, HttpWebResponse response, List<Tuple<string, string>> headers, DateTime? expires)
        {
        	var newEntry = new CacheEntry
        	{
        	    Expires = expires,
        	    DateStored = DateTime.Now,
        	    Headers = headers,
        	    Key = new CacheKey(request.RequestUri.AbsoluteUri, request.UserAgent, profile),
        	    StatusCode = response.StatusCode,
        	    StatusDescription = response.StatusDescription
        	};
        	if (response.ContentLength > 0)
        	{
        		newEntry.ResponseBytes = new byte[response.ContentLength];
        	}
            return newEntry;
        }

        public void AddData(CacheEntry entry)
        {
            var key = entry.Key.ToString();
            var cacheItemPolicy = new CacheItemPolicy();
            if (entry.Expires.HasValue)
            {
                cacheItemPolicy.AbsoluteExpiration = entry.Expires.Value;
            }

            _cache.AddOrGetExisting(key, entry, cacheItemPolicy, _regionName);
        }

        public Boolean CanCache(WebHeaderCollection headers, ref DateTime? expires)
        {
            foreach (var headerKey in headers.AllKeys)
            {
                var headerValue = headers[headerKey].ToLower();
                switch (headerKey.ToLower())
                {
                    case "cache-control":
                        if (headerValue.Contains("max-age"))
                        {
                            int seconds;
                            if (int.TryParse(headerValue, out seconds))
                            {
                                if (seconds < 1)
                                {
                                	return false;
                                }
                                var expiryDate = DateTime.Now.AddSeconds(seconds);
                                if (!expires.HasValue || expires.Value < expiryDate)
                                {
                                	expires = expiryDate;
                                }
                            }
                        }

                        if (headerValue.Contains("private") || headerValue.Contains("no-cache"))
                        {
                        	return false;
                        }
                        else if (headerValue.Contains("public") || headerValue.Contains("no-store"))
                        {
                        	return true;
                        }

                        break;

                    case "pragma":

                        if (headerValue == "no-cache")
                        {
                        	return false;
                        }

                        break;
                    case "expires":
                        DateTime dExpire;
                        if (DateTime.TryParse(headerValue, out dExpire))
                        {
                            if (!expires.HasValue || expires.Value < dExpire)
                            {
                            	expires = dExpire;
                            }
                        }
                        break;
                }
            }
            return true;
        }
    }
}
