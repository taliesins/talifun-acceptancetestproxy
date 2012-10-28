using System.Web;

namespace Talifun.AcceptanceTestProxy.UrlRewriter.Wrappers
{
    public class HttpResponse : HttpResponseBase
    {
        public HttpResponse()
        {
        }

    	private readonly HttpCookieCollection _cookies = new HttpCookieCollection();
		public override HttpCookieCollection Cookies
		{
			get { return _cookies; }
		}

		public override string StatusDescription { get; set; }
		public override int StatusCode { get; set; }
		public override int SubStatusCode { get; set; }
    	public override string ContentType { get; set; }
    }
}
