using System.Security.Cryptography.X509Certificates;

namespace Talifun.AcceptanceTestProxy.Certificates
{
    public interface ICertificateManager
    {
        X509Certificate2 GetSiteCertificate(CertificateGenerator certificateGenerator, string certificatePath, string host, string password);
    }
}