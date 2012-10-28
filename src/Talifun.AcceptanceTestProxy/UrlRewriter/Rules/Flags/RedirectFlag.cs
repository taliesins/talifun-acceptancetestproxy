/** 
 * Copyright (C) 2007-2010 Nicholas Berardi, Managed Fusion, LLC (nick@managedfusion.com)
 * 
 * <author>Nicholas Berardi</author>
 * <author_email>nick@managedfusion.com</author_email>
 * <company>Managed Fusion, LLC</company>
 * <product>Url Rewriter and Reverse Proxy</product>
 * <license>Microsoft Public License (Ms-PL)</license>
 * <agreement>
 * This software, as defined above in <product />, is copyrighted by the <author /> and the <company />, all defined above.
 * 
 * For all binary distributions the <product /> is licensed for use under <license />.
 * For all source distributions please contact the <author /> at <author_email /> for a commercial license.
 * 
 * This copyright notice may not be removed and if this <product /> or any parts of it are used any other
 * packaged software, attribution needs to be given to the author, <author />.  This can be in the form of a textual
 * message at program startup or in documentation (online or textual) provided with the packaged software.
 * </agreement>
 * <product_url>http://www.managedfusion.com/products/url-rewriter/</product_url>
 * <license_url>http://www.managedfusion.com/products/url-rewriter/license.aspx</license_url>
 */

using System;
using System.Net;
using log4net;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags
{
	public class RedirectFlag : IRuleFlag
	{
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RedirectFlag));

		/// <summary>
		/// Initializes a new instance of the <see cref="RedirectFlag"/> class.
		/// </summary>
		/// <param name="statusCode">The status code.</param>
		public RedirectFlag(int statusCode)
		{
			if (statusCode < 300 || statusCode > 307)
			{
			    throw new ArgumentOutOfRangeException("statusCode", statusCode, "statusCode should be between 300 - 307.");
			}

			Type = ((int)statusCode).ToString();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RedirectFlag"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public RedirectFlag(string type)
		{
			if (string.IsNullOrEmpty(type))
			{
			    type = "302";
			}

			if (string.Equals(type, "permanent", StringComparison.InvariantCultureIgnoreCase))
			{
			    type = HttpStatusCode.MovedPermanently.ToString();
			}
			else if (string.Equals(type, "temp", StringComparison.InvariantCultureIgnoreCase))
			{
			    type = HttpStatusCode.RedirectKeepVerb.ToString();
			}

			var statusCode = 0U;

			try
			{
				if (!UInt32.TryParse(type, out statusCode))
				{
					var code = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), type, true);
					statusCode = (uint)code;
				}
			}
			catch (Exception ex)
			{
                Logger.Error("Could not create a redirect status from " + type, ex);
			}

			if (statusCode < 300U || statusCode > 307U)
			{
			    throw new ArgumentOutOfRangeException("type", type, "type resulted in a status code of " + statusCode + " and it should be between 300 - 307.");
			}

			Type = ((int)statusCode).ToString();
		}

		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		/// <value>The type.</value>
		public string Type { get; private set; }

		#region IRuleFlag Members

		/// <summary>
		/// Applies the specified context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public RuleFlagProcessorResponse Apply(RuleContext context)
		{
            Logger.InfoFormat("Output: {0} Redirect to {1}", Type, context.SubstitutedUrl);

		    context.CurrentUrl = context.SubstitutedUrl;

			return RuleFlagProcessorResponse.ExitRuleSet;
		}

		#endregion
	}
}
