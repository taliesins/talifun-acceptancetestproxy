using System.Runtime.Caching;
using System.Security.Cryptography.X509Certificates;

namespace Talifun.AcceptanceTestProxy.Certificates
{
    public class CertificateCache
    {
        private static readonly string RegionName = null;
        private static readonly ObjectCache Cache = MemoryCache.Default;

        public X509Certificate2 GetCertificate(string host)
        {
            var entry = Cache.Get(host, RegionName) as X509Certificate2;
            return entry;
        }

        public void AddCertificate(string host, X509Certificate2 certificate)
        {
            var cacheItemPolicy = new CacheItemPolicy();
            Cache.AddOrGetExisting(host, certificate, cacheItemPolicy, RegionName);
        }
    }
}
