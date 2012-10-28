using System.Linq;
using Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Rules
{
	public static class RuleFlagsProcessor
	{
		/// <summary>
		/// Determines whether [has not for internal sub requests] [the specified flags].
		/// </summary>
		/// <param name="flags">The flags.</param>
		/// <returns>
		/// 	<see langword="true"/> if [has not for internal sub requests] [the specified flags]; otherwise, <see langword="false"/>.
		/// </returns>
		public static bool HasNotForInternalSubRequests(IRuleFlagProcessor flags)
		{
			return flags.OfType<NotForInternalSubRequestsFlag>().Any();
		}

		/// <summary>
		/// Determines whether [has no case] [the specified flags].
		/// </summary>
		/// <param name="flags">The flags.</param>
		/// <returns>
		/// 	<see langword="true"/> if [has no case] [the specified flags]; otherwise, <see langword="false"/>.
		/// </returns>
		public static bool HasNoCase(IRuleFlagProcessor flags)
		{
			return flags.OfType<NoCaseFlag>().Any();
		}

		/// <summary>
		/// Determines whether [has no case] [the specified flags].
		/// </summary>
		/// <param name="flags">The flags.</param>
		/// <returns>
		/// 	<see langword="true"/> if [has no case] [the specified flags]; otherwise, <see langword="false"/>.
		/// </returns>
		public static bool HasChain(IRuleFlagProcessor flags)
		{
			return flags.OfType<ChainFlag>().Any();
		}

		/// <summary>
		/// Determines whether [has no escape] [the specified flags].
		/// </summary>
		/// <param name="flags">The flags.</param>
		/// <returns>
		/// 	<see langword="true"/> if [has no escape] [the specified flags]; otherwise, <see langword="false"/>.
		/// </returns>
		public static bool HasNoEscape(IRuleFlagProcessor flags)
		{
			return flags.OfType<NoEscapeFlag>().Any();
		}
	}
}
