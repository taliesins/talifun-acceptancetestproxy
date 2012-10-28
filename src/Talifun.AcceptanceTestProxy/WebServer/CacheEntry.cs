using System;
using System.Collections.Generic;
using System.Net;

namespace Talifun.AcceptanceTestProxy.WebServer
{
    public class CacheEntry
    {
        public CacheKey Key { get; set; }
        public DateTime? Expires { get; set; }
        public DateTime DateStored { get; set; }
        public byte[] ResponseBytes { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public List<Tuple<string, string>> Headers { get; set; }
        public bool FlagRemove { get; set; }
    }
}
