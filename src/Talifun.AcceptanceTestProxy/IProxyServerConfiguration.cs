using System.Net;

namespace Talifun.AcceptanceTestProxy
{
    public interface IProxyServerConfiguration
    {
        int RequestTimeout { get; }
        string CaCertificateFileName { get; }
        string CaKeyPairFileName { get; }
        IPAddress ListeningIpInterface { get; }
        int ListeningPort { get; }
        string CertificatePath { get; }
        string CertificatePassword { get; }
        bool EnableCaching { get; }
        bool DumpHeaders { get; set; }
        bool DumpPostData { get; set; }
        bool DumpResponseData { get; set; }
    }
}