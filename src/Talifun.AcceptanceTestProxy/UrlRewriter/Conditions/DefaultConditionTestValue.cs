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

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Conditions
{
	/// <summary>
	/// 
	/// </summary>
	public class DefaultConditionTestValue : IConditionTestValue
	{
		private string _test;

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultConditionTestValue"/> class.
		/// </summary>
		protected internal DefaultConditionTestValue()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultConditionTestValue"/> class.
		/// </summary>
		/// <param name="test">The test.</param>
		public DefaultConditionTestValue(string test)
		{
			((IConditionTestValue)this).Init(test);
		}

		#region IConditionPredicate Members

		/// <summary>
		/// Inits the specified test.
		/// </summary>
		/// <param name="test">The test.</param>
		void IConditionTestValue.Init(string test)
		{
			_test = test;
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Test
		{
			get { return _test; }
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public string GetValue(ConditionContext context)
		{
			return Pattern.Replace(_test, context);
		}

		#endregion

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			return Test;
		}
	}
}
