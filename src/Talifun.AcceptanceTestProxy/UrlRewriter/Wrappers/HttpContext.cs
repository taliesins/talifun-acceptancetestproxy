using System.Net;
using System.Web;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Wrappers
{
	public class HttpContext : HttpContextBase
	{
		private readonly HttpRequestBase _request;
	    private readonly HttpResponseBase _response;
		public HttpContext(HttpWebRequest request)
		{
		    _request = new HttpRequest(request);
            _response = new HttpResponse();
		}

	    public override HttpRequestBase Request
		{
			get
			{
				return _request;
			}
		}

        public override HttpResponseBase Response
        {
            get
            {
                return _response;
            }
        }
	}
}
