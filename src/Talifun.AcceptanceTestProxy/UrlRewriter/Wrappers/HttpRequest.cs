using System.Net;
using System.Web;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Wrappers
{
	public class HttpRequest : HttpRequestBase
	{
		private readonly HttpWebRequest _request;
		public HttpRequest(HttpWebRequest request)
		{
			_request = request;
		}

		public override string RawUrl
		{
			get
			{
				return _request.RequestUri.ToString();
			}
		}

		public override System.Uri Url
		{
			get { return _request.RequestUri; }
		}

        public override System.Collections.Specialized.NameValueCollection Headers
        {
            get { return _request.Headers; }
        }
	}
}
