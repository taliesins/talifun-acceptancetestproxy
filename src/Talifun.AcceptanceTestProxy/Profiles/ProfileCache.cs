using System.Runtime.Caching;

namespace Talifun.AcceptanceTestProxy.Profiles
{
	public class ProfileCache
	{
		private static readonly string RegionName = null;
		private static readonly ObjectCache Cache = MemoryCache.Default;

		public Profile GetProfile(string profileName)
		{
			var entry = Cache.Get(profileName, RegionName) as Profile;
			return entry;
		}

		public void AddProfile(string profileName, Profile profile)
		{
			var cacheItemPolicy = new CacheItemPolicy();
			Cache.AddOrGetExisting(profileName, profile, cacheItemPolicy, RegionName);
		}
	}
}
