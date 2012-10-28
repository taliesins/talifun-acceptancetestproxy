using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1.X509;
using Talifun.AcceptanceTestProxy.Certificates;
using Talifun.AcceptanceTestProxy.Profiles;
using Talifun.AcceptanceTestProxy.WebServer;

namespace Talifun.AcceptanceTestProxy
{
    public class Program
    {
        private static void PrintUsageInfo()
        {
            Console.WriteLine("AcceptanceTestProxy server usage: ");
            Console.WriteLine("\t-? = this help");
            Console.WriteLine("\t-h = dump headers to standard output");
            Console.WriteLine("\t-p = dump post data to standard output");
            Console.WriteLine("\t-r = dump response data to standard output (WARNING: redirect your output to a file)");
            Console.WriteLine();
            Console.WriteLine("\tNote: Data dumping kills performance");
            Console.WriteLine();
            Console.WriteLine("\tDisclaimer: This proxy server is for testing and development purposes only.");
            Console.WriteLine("\t            Installing this server on a network without the knowledge of others is not");
            Console.WriteLine("\t            condoned by the author. This proxy server presents a security risk for");
            Console.WriteLine("\t            users who do not understand SSL certificates and browser security.");
            Console.WriteLine();
            Console.WriteLine("\tAuthor: Taliesin Sisson <tali@talifun.com>");
            Console.WriteLine();
        }

        private static void CreateCaCertificate(CertificateGenerator certificateGenerator, IProxyServerConfiguration proxyServerConfiguration)
		{
			var caKeyPair = certificateGenerator.GetKeyPair();

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.C] = "UK";
			caCertificateDetails[X509Name.O] = "Acceptance Test Proxy Organization";
			caCertificateDetails[X509Name.OU] = "Testing Department";
			//caCertificateDetails[X509Name.DnQualifier]; //populatated automatically from CN
			caCertificateDetails[X509Name.ST] = "London";
			caCertificateDetails[X509Name.CN] = "AcceptanceTestProxy CA";
			//caCertificateDetails[X509Name.SerialNumber] = CaCertificateName;  //populatated automatically

			//RFC 5208
			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.C);
			caCertificateDetailsOrder.Add(X509Name.O);
			caCertificateDetailsOrder.Add(X509Name.OU);
			//caCertificateDetailsOrder.Add(X509Name.DnQualifier);
			caCertificateDetailsOrder.Add(X509Name.ST);
			caCertificateDetailsOrder.Add(X509Name.CN);
			//caCertificateDetailsOrder.Add(X509Name.SerialNumber);

			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

            var caKeyPairFileName = Path.Combine(proxyServerConfiguration.CertificatePath, proxyServerConfiguration.CaKeyPairFileName);
			if (File.Exists(caKeyPairFileName))
			{
				File.Delete(caKeyPairFileName);
			}
			var privateKeyText = certificateGenerator.ExportKeyPair(caKeyPair);
			File.WriteAllText(caKeyPairFileName, privateKeyText);

            var caCertificateFileName = Path.Combine(proxyServerConfiguration.CertificatePath, proxyServerConfiguration.CaCertificateFileName);
			if (File.Exists(caCertificateFileName))
			{
				File.Delete(caCertificateFileName);
			}
			var certificateText = certificateGenerator.ExportCertificate(caCertificate);
			File.WriteAllText(caCertificateFileName, certificateText);
		}

        static void Main(string[] args)
        {
			log4net.Config.XmlConfigurator.Configure();

            var proxyServerConfiguration = new ProxyServerConfiguration()
                {
                    DumpHeaders = false,
                    DumpPostData = false,
                    DumpResponseData = false
                };
  
            if (args.Length > 0)
            {
                if (args.Length <= 3)
                {
                    var argRegHelp = new Regex(@"^(/|-)\?$");
                    var argRexH = new Regex("^(/|-)h$");
                    var argRexP = new Regex("^(/|-)p$");
                    var argRexR = new Regex("^(/|-)r$");

                    foreach (var s in args)
                    {
                        if (argRexH.IsMatch(s.ToLower()))
                        {
                            proxyServerConfiguration.DumpHeaders = true;
                        }
                        else if (argRexP.IsMatch(s.ToLower()))
                        {
                            proxyServerConfiguration.DumpPostData = true;
                        }
                        else if (argRexR.IsMatch(s.ToLower()))
                        {
                            proxyServerConfiguration.DumpResponseData = true;
                        }
                        else
                        {
                            PrintUsageInfo();
                            return;
                        }
                    }
                }
                else if (args.Length > 4) 
                {
                    PrintUsageInfo();
                    return;
                }
            }

            var proxyCache = new ProxyCache();
            var profileCache = new ProfileCache();
            var profileManager = new ProfileManager(profileCache);
            var certificateCache = new CertificateCache();
            var certificateManager = new CertificateManager(certificateCache);
            var certificateGenerator = new CertificateGenerator();

            var proxyServer = new ProxyServer(proxyServerConfiguration, profileManager, proxyCache, certificateGenerator, certificateManager);

            var caCertificateFileName = Path.Combine(proxyServerConfiguration.CertificatePath, proxyServerConfiguration.CaCertificateFileName);
            if (!File.Exists(caCertificateFileName))
            {
                CreateCaCertificate(certificateGenerator, proxyServerConfiguration);
            }

			if (proxyServer.Start())
            {
                Console.WriteLine("Server started on {0}:{1}...Press enter key to end", proxyServerConfiguration.ListeningIpInterface, proxyServerConfiguration.ListeningPort);
                Console.ReadLine();
                Console.WriteLine("Shutting down");
				proxyServer.Stop();
                Console.WriteLine("Server stopped...");
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
