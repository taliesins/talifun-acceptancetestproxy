using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Talifun.AcceptanceTestProxy.Certificates
{
	public class CertificateGenerator
	{
		public const string SignatureAlgorithm = "SHA256withRSA";

		#region Key Pair
		/// <summary>
		/// Get a randomly generated key pair to use for creating certificates.
		/// </summary>
		/// <returns>Asymmetric key pair</returns>
		public AsymmetricCipherKeyPair GetKeyPair()
		{
			var kpgen = new RsaKeyPairGenerator();
			kpgen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 1024));
			var kp = kpgen.GenerateKeyPair();

			return kp;
		}

		/// <summary>
		/// PEM representation of the key pair
		/// </summary>
		/// <param name="keyPair">Key value pair</param>
		/// <returns>Key pair in pem format</returns>
		public string ExportKeyPair(AsymmetricCipherKeyPair keyPair)
		{
			// using Bouncy Castle, so that we are 100% sure that the result is exaclty the same as:
			// openssl pkcs12 -in filename.pfx -nocerts -out privateKey.pem
			// openssl rsa -in privateKey.pem -out private.pem

			using (var memoryStream = new MemoryStream())
			{
				using (var streamWriter = new StreamWriter(memoryStream))
				{
					var pemWriter = new PemWriter(streamWriter);
					pemWriter.WriteObject(keyPair.Private);
					streamWriter.Flush();

					// Here is the output with ---BEGIN RSA PRIVATE KEY---
					// that should be exactly the same as in private.pem
					return Encoding.ASCII.GetString(memoryStream.GetBuffer());
				}
			}
		}

		/// <summary>
		/// PEM representation of the key pair
		/// </summary>
		/// <param name="certificate">Certificate to get key pair from</param>
		/// <returns>Key pair in pem format</returns>
		public string ExportKeyPair(X509Certificate2 certificate)
		{
			// Now you have your private key in binary form as you wanted
			// You can use rsa.ExportParameters() or rsa.ExportCspBlob() to get you bytes
			// depending on format you need them in
			var rsa = (RSACryptoServiceProvider)certificate.PrivateKey;

			var keyPair = DotNetUtilities.GetRsaKeyPair(rsa);

			return ExportKeyPair(keyPair);
		}

		/// <summary>
		/// Retrieves the key pair from PEM format
		/// </summary>
		/// <param name="keyPairPemText">Key pair in pem format</param>
		/// <returns>Key pair</returns>
		public AsymmetricCipherKeyPair ImportKeyPair(string keyPairPemText)
		{
			using (var textReader = new StringReader(keyPairPemText))
			{
				var pemReader = new PemReader(textReader);
				var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
				return keyPair;
			}
		}
		#endregion

		#region Certificate
		/// <summary>
		/// Generate a CA certificate
		/// </summary>
		/// <param name="keyPair">Asymmetric key pair to use for generating certificate</param>
		/// <param name="certificateDetails">Certificate details </param>
		/// <param name="certificateDetailsOrder">Order certificate details are created</param>
		/// <returns>A self signed X509 certificate</returns>
		public X509Certificate GenerateCaCertificate(AsymmetricCipherKeyPair keyPair, IDictionary certificateDetails, IList certificateDetailsOrder)
		{
			var startDate = DateTime.UtcNow;              // time from which certificate is valid
			var expiryDate = startDate.AddYears(20);      // time after which certificate is not valid
			var serialNumber = BigInteger.ProbablePrime(120, new Random());

			var certName = new X509Name(certificateDetailsOrder, certificateDetails);
			
			var x509V1CertificateGenerator = new X509V1CertificateGenerator();
			x509V1CertificateGenerator.SetSerialNumber(serialNumber);
			x509V1CertificateGenerator.SetIssuerDN(certName);
			x509V1CertificateGenerator.SetNotBefore(startDate);
			x509V1CertificateGenerator.SetNotAfter(expiryDate);
			x509V1CertificateGenerator.SetSubjectDN(certName); // note: same as issuer
			x509V1CertificateGenerator.SetPublicKey(keyPair.Public);
			x509V1CertificateGenerator.SetSignatureAlgorithm(SignatureAlgorithm);

			var newCert = x509V1CertificateGenerator.Generate(keyPair.Private);
			return newCert;
		}

		/// <summary>
		/// Generate a certificate signed by a CA certificate
		/// </summary>
		/// <param name="caKeyPair">Asymmetric key pair used to generate CA certificate</param>
		/// <param name="caCertificate">CA certificate</param>
		/// <param name="keyPair">Asymmetric key pair to use for generating certificate</param>
		/// <param name="certificateDetails">Certificate details </param>
		/// <param name="certificateDetailsOrder">Order certificate details are created</param>
		/// <returns></returns>
		public X509Certificate GenerateCertificateSignedWithCaCertificate(AsymmetricCipherKeyPair caKeyPair, X509Certificate caCertificate, AsymmetricCipherKeyPair keyPair, IDictionary certificateDetails, IList certificateDetailsOrder)
		{
			var startDate = DateTime.UtcNow.AddYears(-1);              // time from which certificate is valid
			var expiryDate = startDate.AddYears(20);      // time after which certificate is not valid
			var serialNumber = BigInteger.ProbablePrime(120, new Random());
			var certName = new X509Name(certificateDetailsOrder, certificateDetails);

			var x509V3CertificateGenerator = new X509V3CertificateGenerator();
			x509V3CertificateGenerator.SetSerialNumber(serialNumber);
			x509V3CertificateGenerator.SetSubjectDN(certName);
			x509V3CertificateGenerator.SetIssuerDN(caCertificate.SubjectDN);
			x509V3CertificateGenerator.SetNotBefore(startDate);
			x509V3CertificateGenerator.SetNotAfter(expiryDate);
			x509V3CertificateGenerator.SetSignatureAlgorithm(SignatureAlgorithm);
			x509V3CertificateGenerator.SetPublicKey(keyPair.Public);

			//Public key to use
			x509V3CertificateGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new SubjectKeyIdentifierStructure(keyPair.Public));

			//CA Certificate used to sign certificate
			x509V3CertificateGenerator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifierStructure(caCertificate));

			//Sign it with CA private key
			var newCert = x509V3CertificateGenerator.Generate(caKeyPair.Private);

			return newCert;
		}

		/// <summary>
		/// Retrieves the certificate from Pem format.
		/// </summary>
		/// <param name="certificateText">Certificate in Pem format</param>
		/// <returns>An X509 certificate</returns>
		public X509Certificate ImportCertificate(string certificateText)
		{
			using (var textReader = new StringReader(certificateText))
			{
				var pemReader = new PemReader(textReader);
				var certificate = (X509Certificate)pemReader.ReadObject();
				return certificate;
			}
		}

		/// <summary>
		/// PEM representation of X509 certificate
		/// </summary>
		/// <param name="certificate">Certificate to get PEM representation for</param>
		/// <returns>PEM representation for certificate</returns>
		public string ExportCertificate(X509Certificate certificate)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var streamWriter = new StreamWriter(memoryStream))
				{
					var pemWriter = new PemWriter(streamWriter);
					pemWriter.WriteObject(certificate);
					streamWriter.Flush();
					return Encoding.ASCII.GetString(memoryStream.GetBuffer());
				}
			}
		}
		#endregion

		#region Pkcs12 format certificates - certificate in this format contains private key

		/// <summary>
		/// Export an x509 certificate to Pfx container with private key.
		/// </summary>
		/// <param name="certificate">Certificate to convert</param>
		/// <param name="keyPair">Private key to embed</param>
		/// <param name="password">Password to use</param>
		/// <returns></returns>
		public byte[] ExportPfxCertificateWithPrivateKey(X509Certificate certificate, AsymmetricCipherKeyPair keyPair, string password)
		{
			var netCertificate = DotNetUtilities.ToX509Certificate(certificate);

			var certificateData = netCertificate.Export(X509ContentType.Pfx, password);
			var privateKey = (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)keyPair.Private;

			var rsaKeyParameters = DotNetUtilities.ToRSAParameters(privateKey);
			var cspParameters = new CspParameters();
			cspParameters.KeyContainerName = "KeyContainer";
			var rsaCryptoServiceProvider = new RSACryptoServiceProvider(cspParameters);
			rsaCryptoServiceProvider.ImportParameters(rsaKeyParameters);

			var netCertificateV2 = new X509Certificate2();
			netCertificateV2.Import(certificateData, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
			netCertificateV2.PrivateKey = rsaCryptoServiceProvider;
			certificateData = netCertificateV2.Export(X509ContentType.Pfx, password);

			return certificateData;
		}


		/// <summary>
		/// Export a X509 certificate to Pkcs12 container.
		/// </summary>
		/// <param name="certificate">Certificate to export.</param>
		/// <returns>A certificate in Pkcs12 container.</returns>
		/// <remarks>A pkcs12 certificate includes the private key in the certificate.</remarks>
		public byte[] ExportPkcs12Certificate(X509Certificate certificate)
		{
			return DotNetUtilities.ToX509Certificate(certificate).Export(X509ContentType.Pkcs12);
		}

		/// <summary>
		/// Export a X509 certificate to a password protected Pkcs12 container.
		/// </summary>
		/// <param name="certificate">Certificate to export.</param>
		/// <param name="password">Password</param>
		/// <returns>A certificate in Pkcs12 container.</returns>
		/// <remarks>A pkcs12 certificate includes the private key in the certificate.</remarks>
		public byte[] ExportPkcs12Certificate(X509Certificate certificate, string password)
		{
			return DotNetUtilities.ToX509Certificate(certificate).Export(X509ContentType.Pkcs12, password);
		}

		/// <summary>
		/// Retrieves the certificate in .Net certificate format.
		/// </summary>
		/// <returns>An X509 certificate</returns>
		public X509Certificate ImportPkcs12Certificate(byte[] certificate)
		{
			var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate();
			dotNetCertificate.Import(certificate);
			
			return DotNetUtilities.FromX509Certificate(dotNetCertificate);
		}

		/// <summary>
		/// Retrieves the certificate in .Net certificate format.
		/// </summary>
		/// <returns>An X509 certificate</returns>
		public X509Certificate ImportPkcs12Certificate(byte[] certificate, string password)
		{
			var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate();
			dotNetCertificate.Import(certificate, password, X509KeyStorageFlags.Exportable);

			return DotNetUtilities.FromX509Certificate(dotNetCertificate);
		}
		#endregion
	}
}
