using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Talifun.AcceptanceTestProxy.Certificates;
using Talifun.AcceptanceTestProxy.Connections;
using Talifun.AcceptanceTestProxy.Profiles;
using Talifun.AcceptanceTestProxy.WebServer;
using log4net;

namespace Talifun.AcceptanceTestProxy
{
    public class ProxyServer : IProxyServer
    {
        private readonly IProxyServerConfiguration _proxyServerConfiguration;
        private readonly IProfileManager _profileManager;
        private readonly IProxyCache _proxyCache;
        private readonly CertificateGenerator _certificateGenerator;
        private readonly ICertificateManager _certificateManager;

        private const int BufferSize = 8192;
        private static readonly char[] SemiSplit = new char[] { ';' };
        private static readonly char[] EqualSplit = new char[] { '=' };
        private static readonly string[] ColonSpaceSplit = new string[] { ": " };
        private static readonly char[] SpaceSplit = new char[] { ' ' };
        private static readonly char[] CommaSplit = new char[] { ',' };
        private static readonly Regex CookieSplitRegEx = new Regex(@",(?! )");

        private static readonly object OutputLockObj = new object();
		private static readonly ILog _logger = LogManager.GetLogger(typeof(ProxyServer));

        private PortServer _portServer;

        public ProxyServer(IProxyServerConfiguration proxyServerConfiguration, IProfileManager profileManager, IProxyCache proxyCache, CertificateGenerator certificateGenerator, ICertificateManager certificateManager)
        {
            _proxyServerConfiguration = proxyServerConfiguration;
            _profileManager = profileManager;
            _proxyCache = proxyCache;
            _certificateGenerator = certificateGenerator;
            _certificateManager = certificateManager;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        public bool Start()
        {
            try
            {




                _portServer = new PortServer(ProcessRequest, _proxyServerConfiguration.ListeningIpInterface, _proxyServerConfiguration.ListeningPort);
                _portServer.Start("test");
            }
            catch (Exception ex)
            {
				_logger.Error(ex.Message);
                return false;
            }

            return true;
        }

        public void Stop()
        {
            _portServer.StopAll();
            _portServer = null;
        }

        private Task ProcessRequest(TcpClient client, CancellationToken cancellationToken)
        {
            var requestClient = client;
            var requestClientStream = requestClient.GetStream();
            var requestCancellationToken = cancellationToken;

            var task = Task.Factory.StartNew(ant => DoHttpProcessing(requestClientStream, requestCancellationToken), requestCancellationToken);
            // Set up centralized error handling and cleanup:
            task.ContinueWith(ant => _logger.Warn(ant.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(ant =>
            {
                if (requestClientStream != null) requestClientStream.Close();
                if (requestClient != null) requestClient.Close();
            });

            return task;
        }

        private void DoHttpProcessing(Stream clientStream, CancellationToken cancellationToken)
        {
            var outStream = clientStream; //use this stream for writing out - may change if we use ssl
            SslStream sslStream = null;
            var clientStreamReader = new StreamReader(clientStream);
            CacheEntry cacheEntry = null;

            if (_proxyServerConfiguration.DumpHeaders || _proxyServerConfiguration.DumpPostData || _proxyServerConfiguration.DumpResponseData)
            {
                //make sure that things print out in order - NOTE: this is bad for performance
                Monitor.TryEnter(OutputLockObj, TimeSpan.FromMilliseconds(-1.0));
            }
                
            try
            {
                //read the first line HTTP command
                var httpCmd = clientStreamReader.ReadLine();
                if (string.IsNullOrEmpty(httpCmd))
                {
                    clientStreamReader.Close();
                    clientStream.Close();
                    return;
                }

                //break up the line into three components
                var splitBuffer = httpCmd.Split(SpaceSplit, 3);
            	var method = splitBuffer[0];
            	var scheme = method.ToUpper() == "CONNECT" ? "https://": "http://";
                var remoteUri = splitBuffer[1];
                if (!remoteUri.Contains("://"))
                {
                    remoteUri = scheme + remoteUri;
                }
                var version = new Version(1, 0);

				//construct the web request that we are going to issue on behalf of the client.
				var request = (HttpWebRequest)HttpWebRequest.Create(remoteUri);

				//read the request headers from the client and copy them to our request
				var contentLen = ReadRequestHeaders(clientStreamReader, request);

				string proxyUsername = null;
				if (string.IsNullOrEmpty(request.Headers[HttpRequestHeader.ProxyAuthorization]))
				{
					//Must authenticate to proxy
					ResponseProxyAuthenticationRequired(outStream);
					return;
				}
				else
				{
					proxyUsername = GetUsername(request);
					//Proxy-Authorization must not be passed onto downward streams
					request.Headers.Remove(HttpRequestHeader.ProxyAuthorization);
				}

                if (_proxyServerConfiguration.DumpHeaders)
				{
					_logger.InfoFormat("Proxy authenticated username: {0}", proxyUsername);
				}

                var rewrittenUri = _profileManager.HandleRequest(proxyUsername, request);

                if (rewrittenUri != null)
                {
                    var rewrittenRequest = (HttpWebRequest)HttpWebRequest.Create(rewrittenUri);

                    scheme = rewrittenUri.Scheme + "://";
                    remoteUri = rewrittenRequest.ToString();

                    //Transfer all headers over to transferred request
                    foreach (var headerKey in request.Headers.AllKeys)
                    {
                        SetHeader(rewrittenRequest, headerKey, request.Headers[headerKey]);
                    }
                 
                    request = rewrittenRequest;
                }

				if (method.ToUpper() == "CONNECT")
				{
					//This is an ssl request so we must intercept it if we want to redirect
					sslStream = ManInMiddleAttack(remoteUri, ref clientStream, ref clientStreamReader);

					outStream = sslStream;

					//read the new http command.
				    httpCmd = clientStreamReader.ReadLine();
				    if (string.IsNullOrEmpty(httpCmd))
					{
						clientStreamReader.Close();
						clientStream.Close();
						sslStream.Close();
						return;
					}

					splitBuffer = httpCmd.Split(SpaceSplit, 3);
					method = splitBuffer[0];
					remoteUri = remoteUri + splitBuffer[1];

					//construct the web request that we are going to issue on behalf of the client.
					request = (HttpWebRequest)HttpWebRequest.Create(remoteUri);

					//read the request headers from the client and copy them to our request
					contentLen = ReadRequestHeaders(clientStreamReader, request);
				}

				request.Method = method;
				request.ProtocolVersion = version;
                request.Proxy = null;
                request.KeepAlive = false;
                request.AllowAutoRedirect = false;
                request.AutomaticDecompression = DecompressionMethods.None;

                if (_proxyServerConfiguration.DumpHeaders)
                {
					_logger.InfoFormat("{0} {1} HTTP/{2}", request.Method, request.RequestUri.AbsoluteUri, request.ProtocolVersion);
                    DumpHeaderCollectionToConsole(request.Headers);
                }

                //using the completed request, check our cache
                switch (method.ToUpper())
                {
                	case "GET":
                        if (_proxyServerConfiguration.EnableCaching && rewrittenUri == null)
						{
							cacheEntry = _proxyCache.GetData(request, proxyUsername);
						}
                		break;
                	case "POST":
                		{
                			var postBuffer = new char[contentLen];
                			int bytesRead;
                			var totalBytesRead = 0;
                			var sw = new StreamWriter(request.GetRequestStream());
                			var loggerOutput = new StringBuilder();
                			while (totalBytesRead < contentLen && (bytesRead = clientStreamReader.ReadBlock(postBuffer, 0, contentLen)) > 0)
                			{
                				totalBytesRead += bytesRead;
                				sw.Write(postBuffer, 0, bytesRead);
                                if (_proxyServerConfiguration.DumpPostData)
                				{
                					loggerOutput.Append(postBuffer, 0, bytesRead);
                				}
                			}

                            if (_proxyServerConfiguration.DumpPostData)
                			{
								_logger.InfoFormat("Post data: {0}", loggerOutput);
                			}

                			sw.Close();
                		}
                		break;
                }

                if (cacheEntry == null)
                {
                    //Console.WriteLine(String.Format("ThreadID: {2} Requesting {0} on behalf of client {1}", request.RequestUri, client.Client.RemoteEndPoint.ToString(), Thread.CurrentThread.ManagedThreadId));
                    request.Timeout = _proxyServerConfiguration.RequestTimeout;

                	HttpWebResponse response;
                	try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException webEx)
                    {
                        response = webEx.Response as HttpWebResponse;
                        _logger.Warn("Unable to perform remote request", webEx);
                    }

                    if (response != null)
                    {
                        ResponseFromForwardedRequest(outStream, request, proxyUsername, response, sslStream != null);
                    }
                }
                else
                {
                    ResponseFromCache(outStream, cacheEntry);
                }
            }
            catch (Exception ex)
            {
				_logger.Error(ex);
            }
            finally
            {
                if (_proxyServerConfiguration.DumpHeaders || _proxyServerConfiguration.DumpPostData || _proxyServerConfiguration.DumpResponseData)
                {
                    //release the lock
                    Monitor.Exit(OutputLockObj);
                }

                clientStreamReader.Close();
                clientStream.Close();
                if (sslStream != null)
                {
                	sslStream.Close();
                }
                if (outStream != null)
                {
                    outStream.Close();
                }
            }
        }

		private SslStream ManInMiddleAttack(string remoteUri, ref Stream clientStream, ref StreamReader clientStreamReader)
		{
			//Browser wants to create a secure tunnel
			//instead = we are going to perform a man in the middle "attack"
			//the user's browser should warn them of the certification errors however.
			//Please note: THIS IS ONLY FOR TESTING PURPOSES - you are responsible for the use of this code

			var connectStreamWriter = new StreamWriter(clientStream);
			connectStreamWriter.WriteLine("HTTP/1.0 200 Connection established");
			connectStreamWriter.WriteLine("Timestamp: {0}", DateTime.Now.ToUniversalTime());
			connectStreamWriter.WriteLine("Proxy-agent: AcceptanceTestProxy");
			connectStreamWriter.WriteLine();
			connectStreamWriter.Flush();

			var sslStream = new SslStream(clientStream, false);
			try
			{
				var host = new Uri(remoteUri).Host;
                var siteCertificate = _certificateManager.GetSiteCertificate(_certificateGenerator, _proxyServerConfiguration.CertificatePath, host, _proxyServerConfiguration.CertificatePassword);

				sslStream.AuthenticateAsServer(siteCertificate, false, SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2, true);
			}
			catch (Exception ex)
			{
                _logger.Warn(ex);
				sslStream.Close();
				clientStreamReader.Close();
				connectStreamWriter.Close();
				clientStream.Close();
				return null;
			}

			//HTTPS server created - we can now decrypt the client's traffic
			clientStream = sslStream;
			clientStreamReader = new StreamReader(sslStream);

			return sslStream;
		}

        /// <summary>
        /// Get the username for a proxy authorization request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
		private string GetUsername(HttpWebRequest request)
		{
		    var proxyAuthorization = request.Headers[HttpRequestHeader.ProxyAuthorization].Split(' ');

		    if (proxyAuthorization.Length != 2 || !proxyAuthorization[0].Equals("basic", StringComparison.InvariantCultureIgnoreCase))
		    {
		        return null;
		    }

		    var usernameAndPassword = DecodeFrom64(proxyAuthorization[1]).Split(':');

            if (usernameAndPassword.Length != 2)
            {
                return null;
            }

            return usernameAndPassword[0];
		}

        /// <summary>
        /// Decode Base64 strings.
        /// </summary>
        /// <param name="encodedData">The String containing the characters to decode.</param>
        /// <returns>A String containing the results of decoding the specified sequence of bytes.</returns>
        public string DecodeFrom64(string encodedData)
        {
            var encodedDataAsBytes = Convert.FromBase64String(encodedData);
            var returnValue = Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }

        private void ResponseFromForwardedRequest(Stream outStream, HttpWebRequest request, string proxyUsername, HttpWebResponse response, bool isSsl)
        {
            MemoryStream cacheStream = null;
            var responseHeaders = ProcessResponse(response); 
            var responseWriter = new StreamWriter(outStream);
            var responseStream = response.GetResponseStream();
            try
            {
                //send the response status and response headers
                WriteResponseStatus(response.StatusCode, response.StatusDescription, responseWriter);
                WriteResponseHeaders(responseWriter, responseHeaders);

                DateTime? expires = null;
                CacheEntry entry = null;
                var canCache = (_proxyServerConfiguration.EnableCaching && !isSsl && _proxyCache.CanCache(response.Headers, ref expires));
                
                if (canCache)
                {
                    entry = _proxyCache.MakeEntry(request, proxyUsername, response, responseHeaders, expires);
                    if (response.ContentLength > 0)
                    {
                        cacheStream = new MemoryStream(entry.ResponseBytes);
                    }
                }

                var buffer = response.ContentLength > 0 ? new byte[response.ContentLength] : new byte[BufferSize];

                int bytesRead;

				var loggerOutput = new StringBuilder();
                while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (cacheStream != null)
                    {
                        cacheStream.Write(buffer, 0, bytesRead);
                    }
                    outStream.Write(buffer, 0, bytesRead);
                    if (_proxyServerConfiguration.DumpResponseData)
                    {
                    	loggerOutput.Append(UTF8Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    }
                }

                if (_proxyServerConfiguration.DumpResponseData)
                {
					_logger.Info(loggerOutput);
                }

                responseStream.Close();

                outStream.Flush();

                if (canCache)
                {
                    _proxyCache.AddData(entry);
                }
            }
            catch (Exception ex)
            {
				_logger.Error(ex);
            }
            finally
            {
                responseStream.Close();
                response.Close();
                responseWriter.Close();
            }

            if (cacheStream != null)
            {
                cacheStream.Flush();
                cacheStream.Close();
            }
        }

        private void ResponseFromCache(Stream outStream, CacheEntry cacheEntry)
        {
            //serve from cache
            var responseWriter = new StreamWriter(outStream);
            try
            {
                WriteResponseStatus(cacheEntry.StatusCode, cacheEntry.StatusDescription, responseWriter);
                WriteResponseHeaders(responseWriter, cacheEntry.Headers);
                if (cacheEntry.ResponseBytes != null)
                {
					var loggerOutput = new StringBuilder();
                    outStream.Write(cacheEntry.ResponseBytes, 0, cacheEntry.ResponseBytes.Length);
                    if (_proxyServerConfiguration.DumpResponseData)
                    {
						loggerOutput.Append(UTF8Encoding.UTF8.GetString(cacheEntry.ResponseBytes));
                    }
                }
                responseWriter.Close();
            }
            catch (Exception ex)
            {
				_logger.Error(ex);
            }
            finally
            {
                responseWriter.Close();
            }
        }

		private void ResponseProxyAuthenticationRequired(Stream outStream)
		{
			var body = string.Empty;

			var headers = new WebHeaderCollection
			{
			    {HttpResponseHeader.ProxyAuthenticate, "Basic realm=\"AcceptanceTestProxy\""},
			    {HttpResponseHeader.ContentType, "text/html"},
			    {HttpResponseHeader.ContentLength, body.Length.ToString(CultureInfo.InvariantCulture)},
			    {HttpResponseHeader.CacheControl, "no-cache"},
			    {HttpResponseHeader.Pragma, "no-cache"}
			};

			var myResponseWriter = new StreamWriter(outStream);
            try
            {
            	WriteResponseStatus(HttpStatusCode.ProxyAuthenticationRequired, "Proxy Authentication Required", myResponseWriter);
				WriteResponseHeaders(myResponseWriter, headers);
            }
			catch (Exception ex)
			{
				_logger.Error(ex);
			}
			finally
			{
				myResponseWriter.Close();
			}
		}

        private List<Tuple<string, string>> ProcessResponse(HttpWebResponse response)
        {
            string value=null;
            string header=null;
            var returnHeaders = new List<Tuple<string, string>>();
            foreach (string s in response.Headers.Keys)
            {
                if (s.ToLower() == "set-cookie")
                {
                    header = s;
                    value = response.Headers[s];
                }
                else
                {
                	returnHeaders.Add(new Tuple<string, string>(s, response.Headers[s]));
                }
            }
            
            if (!string.IsNullOrWhiteSpace(value))
            {
                response.Headers.Remove(header);
                var cookies = CookieSplitRegEx.Split(value);
            	returnHeaders.AddRange(cookies.Select(cookie => new Tuple<string, string>("Set-Cookie", cookie)));
            }
            returnHeaders.Add(new Tuple<string, string>("X-Proxied-By", "AcceptanceTestProxy"));
            return returnHeaders;
        }

        private void WriteResponseStatus(HttpStatusCode code, string description, StreamWriter myResponseWriter)
        {
            var s = string.Format("HTTP/1.0 {0} {1}", (int)code, description);
            myResponseWriter.WriteLine(s);
            if (_proxyServerConfiguration.DumpHeaders)
            {
				_logger.Info(s);
            }
        }

		private void WriteResponseHeaders(StreamWriter myResponseWriter, WebHeaderCollection headers)
		{
			var tupleHeaders = headers.AllKeys.Select(headerKey => new Tuple<string, string>(headerKey, headers[headerKey])).ToList();
			WriteResponseHeaders(myResponseWriter, tupleHeaders);
		}

        private void WriteResponseHeaders(StreamWriter myResponseWriter, List<Tuple<string, string>> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                	myResponseWriter.WriteLine("{0}: {1}", header.Item1,header.Item2);
                }
            }
            myResponseWriter.WriteLine();
            myResponseWriter.Flush();

            if (_proxyServerConfiguration.DumpHeaders)
            {
            	DumpHeaderCollectionToConsole(headers);
            }
        }

        private void DumpHeaderCollectionToConsole(WebHeaderCollection headers)
        {
        	var loggerOutput = new StringBuilder();
            foreach (var s in headers.AllKeys)
            {
				loggerOutput.AppendLine(string.Format("{0}: {1}", s, headers[s]));
            }
			_logger.Info(loggerOutput);
        }

        private void DumpHeaderCollectionToConsole(List<Tuple<string, string>> headers)
        {
			var loggerOutput = new StringBuilder();
            foreach (var header in headers)
            {
				loggerOutput.AppendLine(string.Format("{0}: {1}", header.Item1, header.Item2));
            }
			_logger.Info(loggerOutput);
        }

        private void SetHeader(HttpWebRequest request, string headerKey, string headerValue)
        {
            switch (headerKey.ToLower())
            {
                case "host":
                    request.Host = headerValue;
                    break;
                case "user-agent":
                    request.UserAgent = headerValue;
                    break;
                case "accept":
                    request.Accept = headerValue;
                    break;
                case "referer":
                    request.Referer = headerValue;
                    break;
                case "cookie":
                    request.Headers["Cookie"] = headerValue;
                    break;
                case "proxy-connection":
                case "connection":
                case "keep-alive":
                    //ignore these
                    break;
                case "content-length":
                    //ignore this
                    break;
                case "content-type":
                    request.ContentType = headerValue;
                    break;
                case "if-modified-since":
                    var sb = headerValue.Trim().Split(SemiSplit);
                    DateTime d;
                    if (DateTime.TryParse(sb[0], out d))
                    {
                        request.IfModifiedSince = d;
                    }
                    break;
                default:
                    try
                    {
                        request.Headers.Add(headerKey, headerValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(string.Format("Could not add header {0}", headerKey), ex);
                    }
                    break;
            }
        }

        private int ReadRequestHeaders(StreamReader streamReader, HttpWebRequest request)
        {
            string httpHeaderLineString;
            var contentLength = 0;
            do
            {
                httpHeaderLineString = streamReader.ReadLine();
                if (string.IsNullOrEmpty(httpHeaderLineString))
                {
                	return contentLength;
                }
                var header = httpHeaderLineString.Split(ColonSpaceSplit, 2, StringSplitOptions.None);
                
                if (header[0].ToLower() == "content-length")
                {
                    int.TryParse(header[1], out contentLength);
                }
                else
                {
                    SetHeader(request, header[0], header[1]);
                }

            } while (!string.IsNullOrWhiteSpace(httpHeaderLineString));
            return contentLength;
        }
    }
}