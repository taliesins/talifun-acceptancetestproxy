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

using System.Collections.Generic;
using Talifun.AcceptanceTestProxy.UrlRewriter.Rules.Flags;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Rules
{
	/// <summary>
	/// 
	/// </summary>
	public class RuleFlagProcessor : IRuleFlagProcessor
	{
		private readonly OrderedList<IRuleFlag> _flags;

		/// <summary>
		/// Initializes a new instance of the <see cref="RuleFlagProcessor"/> class.
		/// </summary>
		public RuleFlagProcessor()
		{
			_flags = new OrderedList<IRuleFlag>(Comparison, 5);
		}

		/// <summary>
		/// Performs a comparison between rules inorder to sort them
		/// </summary>
		/// <param name="x">Rule X</param>
		/// <param name="y">Rule Y</param>
		/// <returns></returns>
		private int Comparison(IRuleFlag x, IRuleFlag y)
		{
			var xLast = x is LastFlag;
			var yLast = y is LastFlag;

            //bool xExit = x is ProxyFlag || x is RedirectFlag;
            //bool yExit = y is ProxyFlag || y is RedirectFlag;

            var xExit = x is RedirectFlag;
            var yExit = y is RedirectFlag;

			var isXSpecial = xLast || xExit;
			var isYSpecial = yLast || yExit;

			// neither are a special case
			if (!isXSpecial && !isYSpecial)
			{
			    return 0;
			}
			// y is a special case
			else if (!isXSpecial && isYSpecial)
			{
			    return -1;
			}
			// x is a special case
			else if (isXSpecial && !isYSpecial)
			{
			    return 1;
			}
			// both special case, x is Last and y is Exit
			else if (xLast && yExit)
			{
			    return 1;
			}
			// both special case, y is Last and x is Exit
			else if (xExit && yLast)
			{
			    return -1;
			}
			// they are equal
			else
			{
			    return 0;
			}
		}

		/// <summary>
		/// Applies the specified context.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public RuleFlagProcessorResponse Apply(RuleContext context)
		{
			foreach (var flag in this)
			{
				var response = flag.Apply(context);

				if (response != RuleFlagProcessorResponse.ContinueToNextFlag)
				{
				    return response;
				}
			}

			return RuleFlagProcessorResponse.ContinueToNextRule;
		}

		#region IRuleFlagProcessor Members

		/// <summary>
		/// Begin bulk loading.
		/// </summary>
		public void BeginAdd()
		{
			_flags.BeginAdd();
		}

		/// <summary>
		/// Adds the specified flag.
		/// </summary>
		/// <param name="flag">The flag.</param>
		public void Add(IRuleFlag flag)
		{
			_flags.Add(flag);
		}

		/// <summary>
		/// End bulk loading.
		/// </summary>
		public void EndAdd()
		{
			_flags.EndAdd();
		}

		#endregion

		#region IEnumerable<IRuleFlag> Members

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<IRuleFlag> GetEnumerator()
		{
			return _flags.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _flags.GetEnumerator();
		}

		#endregion
	}
}