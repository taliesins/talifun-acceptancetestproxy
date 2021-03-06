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

using System.IO;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Conditions
{
	/// <summary>
	/// 
	/// </summary>
	public class IsFileWithSizeCondition : ICondition
	{
		#region ICondition Members

		/// <summary>
		/// Gets the pattern.
		/// </summary>
		/// <value>The pattern.</value>
		public Pattern Pattern
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the condition pattern.
		/// </summary>
		/// <value>The condition pattern.</value>
		public IConditionTestValue Test
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the flags.
		/// </summary>
		/// <value>The flags.</value>
		public IConditionFlagProcessor Flags
		{
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the specified value is match.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		/// <returns>
		/// 	<c>true</c> if the specified value is match; otherwise, <c>false</c>.
		/// </returns>
		private bool IsMatch(bool value)
		{
			return Pattern.InvertMatch ? !value : value;
		}

		/// <summary>
		/// Evaluates the specified context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns>
		/// 	<see langword="true"/> if the specified log level is match; otherwise, <see langword="false"/>.
		/// </returns>
		public bool Evaluate(ConditionContext context)
		{
			var test = Test.GetValue(context);

			if (string.IsNullOrEmpty(test))
			{
			    return IsMatch(false);
			}

			if (!test.StartsWith(UrlRewriterManager.AppDomainAppPath) && !test.Contains(":"))
			{
			    test = context.HttpContext.Server.MapPath(test);
			}

			if (File.Exists(test))
			{
				var file = new FileInfo(test);
				return IsMatch(file.Length > 0);
			}

			return IsMatch(false);
		}

		/// <summary>
		/// Inits the specified text.
		/// </summary>
		/// <param name="pattern">The pattern.</param>
		/// <param name="test">The test.</param>
		/// <param name="flags">The flags.</param>
		public void Init(Pattern pattern, IConditionTestValue test, IConditionFlagProcessor flags)
		{
			Pattern = pattern;
			Test = test;
			Flags = flags ?? new ConditionFlagProcessor();
		}

		#endregion
	}
}
