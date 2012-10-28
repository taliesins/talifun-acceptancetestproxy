using System.Collections;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using log4net;

namespace Talifun.AcceptanceTestProxy.Certificates
{
	public class CertificateManager
	{
        private static readonly object CertificateCreatorLock = new object();
		private static readonly ILog Logger = LogManager.GetLogger(typeof(CertificateManager));
		private readonly CertificateCache _certificateCache;

		public CertificateManager(CertificateCache certificateCache)
		{
			_certificateCache = certificateCache;
		}
		
		private static void GenerateSiteCertificate(CertificateGenerator certificateGenerator, AsymmetricCipherKeyPair caKeyPair, Org.BouncyCastle.X509.X509Certificate caCertificate, string certificatePath, string host, string password)
		{
			var keyPair = caKeyPair;

			IDictionary certificateDetails = new Hashtable();
			certificateDetails[X509Name.CN] = host;

			IList certificateDetailsOrder = new ArrayList();
			certificateDetailsOrder.Add(X509Name.CN);

			var certificate = certificateGenerator.GenerateCertificateSignedWithCaCertificate(caKeyPair, caCertificate, keyPair, certificateDetails, certificateDetailsOrder);
			var certificateData = certificateGenerator.ExportPfxCertificateWithPrivateKey(certificate, keyPair, password);
			var certificateFileName = Path.Combine(certificatePath, host + ".pfx");

			File.WriteAllBytes(certificateFileName, certificateData);

			Logger.InfoFormat("Create certificate for host: {0}", host);
		}

		private static X509Certificate2 GetSiteCertificateFromFile(string certificatePath, string subjectName, string password)
		{
			var certificateFileName = Path.Combine(certificatePath, subjectName + ".pfx");
			var netCertificate = new X509Certificate2(certificateFileName, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

			Logger.InfoFormat("Loaded certificate for host: {0}", subjectName);
			return netCertificate;
		}

		public X509Certificate2 GetSiteCertificate(CertificateGenerator certificateGenerator, AsymmetricCipherKeyPair caKeyPair, Org.BouncyCastle.X509.X509Certificate caCertificate, string certificatePath, string host, string password)
		{
			var certificate = _certificateCache.GetCertificate(host);
			if (certificate != null)
			{
				return certificate;
			}

			var filename = Path.Combine(certificatePath, host + ".pfx");

			lock (CertificateCreatorLock)
			{
				if (!File.Exists(filename))
				{
					GenerateSiteCertificate(certificateGenerator, caKeyPair, caCertificate, certificatePath, host, password);
				}

				certificate = GetSiteCertificateFromFile(certificatePath, host, password);
				_certificateCache.AddCertificate(host, certificate);
			}

			return certificate;
		}
	}
}
