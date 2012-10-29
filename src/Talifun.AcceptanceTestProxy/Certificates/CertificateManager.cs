using System.Collections;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using log4net;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Talifun.AcceptanceTestProxy.Certificates
{
	public class CertificateManager : ICertificateManager
	{
        private static readonly object CertificateCreatorLock = new object();
		private static readonly ILog Logger = LogManager.GetLogger(typeof(CertificateManager));
		private readonly CertificateCache _certificateCache;
	    private readonly Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair _caKeyPair;
	    private readonly Org.BouncyCastle.X509.X509Certificate _caCertificate;

		public CertificateManager(CertificateCache certificateCache, AsymmetricCipherKeyPair caKeyPair, X509Certificate caCertificate)
		{
		    _certificateCache = certificateCache;
		    _caKeyPair = caKeyPair;
		    _caCertificate = caCertificate;
		}

	    private void GenerateSiteCertificate(CertificateGenerator certificateGenerator, string certificatePath, string host, string password)
		{
			var keyPair = _caKeyPair;

			IDictionary certificateDetails = new Hashtable();
			certificateDetails[X509Name.CN] = host;

			IList certificateDetailsOrder = new ArrayList();
			certificateDetailsOrder.Add(X509Name.CN);

			var certificate = certificateGenerator.GenerateCertificateSignedWithCaCertificate(_caKeyPair, _caCertificate, keyPair, certificateDetails, certificateDetailsOrder);
			var certificateData = certificateGenerator.ExportPfxCertificateWithPrivateKey(certificate, keyPair, password);
			var certificateFileName = Path.Combine(certificatePath, host + ".pfx");

			File.WriteAllBytes(certificateFileName, certificateData);

			Logger.InfoFormat("Create certificate for host: {0}", host);
		}

		private X509Certificate2 GetSiteCertificateFromFile(string certificatePath, string subjectName, string password)
		{
			var certificateFileName = Path.Combine(certificatePath, subjectName + ".pfx");
			var netCertificate = new X509Certificate2(certificateFileName, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

			Logger.InfoFormat("Loaded certificate for host: {0}", subjectName);
			return netCertificate;
		}

		public X509Certificate2 GetSiteCertificate(CertificateGenerator certificateGenerator, string certificatePath, string host, string password)
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
					GenerateSiteCertificate(certificateGenerator, certificatePath, host, password);
				}

				certificate = GetSiteCertificateFromFile(certificatePath, host, password);
				_certificateCache.AddCertificate(host, certificate);
			}

			return certificate;
		}
	}
}
