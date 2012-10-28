﻿/** 
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

using log4net;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags
{
	public class ServerVariableFlag : IRuleFlag
	{
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerVariableFlag));

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerVariableFlag"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <param name="replace">if set to <c>true</c> [replace].</param>
		public ServerVariableFlag(string name, string value, bool replace)
		{
			Name = name;
			Value = value;
			Replace = replace;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <value>The value.</value>
		public string Value { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="ServerVariableFlag"/> is replace.
		/// </summary>
		/// <value><c>true</c> if replace; otherwise, <c>false</c>.</value>
		public bool Replace { get; private set; }

		#region IRuleFlag Members

		/// <summary>
		/// Applies the specified context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public RuleFlagProcessorResponse Apply(RuleContext context)
		{
			UrlRewriterManager.SetServerVariable(context.HttpContext, Name, Value, Replace);

            Logger.InfoFormat("Set Server Variable: {0}", Name);
			return RuleFlagProcessorResponse.ContinueToNextFlag;
		}

		#endregion
	}
}
