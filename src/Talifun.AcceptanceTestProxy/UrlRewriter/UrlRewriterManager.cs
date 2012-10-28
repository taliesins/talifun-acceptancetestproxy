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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using log4net;

namespace Talifun.AcceptanceTestProxy.UrlRewriter
{
    /// <summary>
    /// 
    /// </summary>
    public static class UrlRewriterManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UrlRewriterManager));

        public static readonly RegexOptions RuleOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant;

        public static string AppDomainAppPath
        {
            get { return HttpRuntime.AppDomainAppPath; }
        }

        public static string ApplicationPhysicalPath
        {
            get { return HostingEnvironment.ApplicationPhysicalPath; }
        }

        /// <summary>
        /// Set the server variable.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="name">The server variable name.</param>
        /// <param name="value">The value.</param>
        public static void SetServerVariable(HttpContextBase context, string name, string value, bool replace)
        {
            try
            {
                // if a replace isn't allowed and there is already a server variable set then exit
                if (!replace && !string.IsNullOrEmpty(context.Request.ServerVariables.Get(name)))
                {
                	return;
                }

                if (HttpRuntime.UsingIntegratedPipeline)
                {
                    context.Request.ServerVariables.Set(name, value);
                }
                else
                {
                    var targetType = typeof(NameValueCollection);

                    // get the property for setting readability
                    var isReadOnlyProperty = targetType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);

                    // set headers as read and write
                    isReadOnlyProperty.SetValue(context.Request.Headers, false, null);

                    var list = new ArrayList();
                    list.Add(value);

                    // get the method to fill in the headers
                    var fillInHeadersCollectionMethod = targetType.GetMethod("BaseSet", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(object) }, null);
                    fillInHeadersCollectionMethod.Invoke(context.Request.Headers, new object[] { name, list });

                    // set headers as read only
                    isReadOnlyProperty.SetValue(context.Request.Headers, true, null);
                }
            }
            catch (SecurityException) { }
            catch (MethodAccessException) { }
            catch (NullReferenceException) { }
        }
    }
}