using System;
using System.Collections.Generic;
using System.Net;

namespace Talifun.AcceptanceTestProxy.WebServer
{
    public interface IProxyCache
    {
        CacheEntry GetData(HttpWebRequest request, string profile);
        CacheEntry MakeEntry(HttpWebRequest request, string profile, HttpWebResponse response, List<Tuple<string, string>> headers, DateTime? expires);
        void AddData(CacheEntry entry);
        Boolean CanCache(WebHeaderCollection headers, ref DateTime? expires);
    }
}