using System.Configuration;
using System.Net;
using Talifun.AcceptanceTestProxy.Certificates;

namespace Talifun.AcceptanceTestProxy
{
    public class ProxyServerConfiguration : IProxyServerConfiguration
    {
        public int RequestTimeout
        {
            get
            {
                var requestTimeout = 15000;
                if (ConfigurationManager.AppSettings["RequestTimeout"] != null)
                {
                    int.TryParse(ConfigurationManager.AppSettings["RequestTimeout"], out requestTimeout);
                }

                return requestTimeout;
            }
        }

        public string CaCertificateFileName
        {
            get
            {
                return ConfigurationManager.AppSettings["CaCertificateFileName"] ?? "AcceptanceTestProxyCa.crt";
            }
        }

        public string CaKeyPairFileName
        {
            get
            {
                return ConfigurationManager.AppSettings["CaKeyPairFileName"] ?? "AcceptanceTestProxyCa.KeyPair.pem";
            }
        }

        public IPAddress ListeningIpInterface
        {
            get
            {
                var addr = IPAddress.Loopback;
                if (ConfigurationManager.AppSettings["ListeningIPInterface"] != null)
                {
                    IPAddress.TryParse(ConfigurationManager.AppSettings["ListeningIPInterface"], out addr);
                }

                return addr;
            }
        }

        public int ListeningPort
        {
            get
            {
                var port = 3128;
                if (ConfigurationManager.AppSettings["ListeningPort"] != null)
                {
                    int.TryParse(ConfigurationManager.AppSettings["ListeningPort"], out port);
                }

                return port;
            }
        }

        public string CertificatePath
        {
            get
            {
                var certificatePath = "Certificates\\";
                if (ConfigurationManager.AppSettings["CertificatePath"] != null)
                {
                    certificatePath = ConfigurationManager.AppSettings["CertificatePath"];
                }

                return certificatePath;
            }
        }

        public string CertificatePassword
        {
            get
            {
                var certificatePassword = string.Empty;
                if (ConfigurationManager.AppSettings["CertificatePassword"] != null)
                {
                    certificatePassword = ConfigurationManager.AppSettings["CertificatePassword"];
                }

                return certificatePassword;
            }
        }

        public bool EnableCaching
        {
            get
            {
                var enableCaching = true;
                if (ConfigurationManager.AppSettings["EnableCaching"] != null)
                {
                    bool.TryParse(ConfigurationManager.AppSettings["EnableCaching"], out enableCaching);
                }

                return enableCaching;
            }
        }

        public Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair CaKeyPair { get; set; }
        public Org.BouncyCastle.X509.X509Certificate CaCertificate { get; set; }
        public bool DumpHeaders { get; set; }
        public bool DumpPostData { get; set; }
        public bool DumpResponseData { get; set; }
    }
}
