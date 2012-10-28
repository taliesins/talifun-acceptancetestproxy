using System.Collections;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.X509;
using Talifun.AcceptanceTestProxy.Certificates;

namespace Talifun.AcceptanceTestProxy.Tests
{
	[TestFixture]
	public class CertificateGeneratorTests
	{
		[Test]
		public void GenerateCaCertificate_SelfSigned_ValidCertificate()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string subjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = subjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			// Act
			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

			// Assert
			Assert.AreEqual("CN=" + subjectName, caCertificate.IssuerDN.ToString()); //Self signed
		}

		[Test]
		public void GenerateCaCertificate_SubjectValid_ValidCertificate()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string subjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = subjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			// Act
			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

			// Assert
			Assert.AreEqual("CN=" + subjectName, caCertificate.SubjectDN.ToString());
		}

		[Test]
		public void GenerateCertificateSignedWithCaCertificate_SignedWithCaCertificate_ValidCertificate()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string caSubjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = caSubjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

			var keyPair = certificateGenerator.GetKeyPair();
			const string subjectName = "www.google.com";

			IDictionary certificateDetails = new Hashtable();
			certificateDetails[X509Name.CN] = subjectName;

			IList certificateDetailsOrder = new ArrayList();
			certificateDetailsOrder.Add(X509Name.CN);

			// Act
			var certificate = certificateGenerator.GenerateCertificateSignedWithCaCertificate(caKeyPair, caCertificate, keyPair, certificateDetails, certificateDetailsOrder);
			
			// Assert
			Assert.AreEqual("CN=" + caSubjectName, certificate.IssuerDN.ToString()); //Signed with CA
		}

		[Test]
		public void GenerateCertificateSignedWithCaCertificate_SubjectValid_ValidCertificate()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string caSubjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = caSubjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

			var keyPair = certificateGenerator.GetKeyPair();
			const string subjectName = "www.google.com";

			IDictionary certificateDetails = new Hashtable();
			certificateDetails[X509Name.CN] = subjectName;

			IList certificateDetailsOrder = new ArrayList();
			certificateDetailsOrder.Add(X509Name.CN);

			// Act
			var certificate = certificateGenerator.GenerateCertificateSignedWithCaCertificate(caKeyPair, caCertificate, keyPair, certificateDetails, certificateDetailsOrder);

			// Assert
			Assert.AreEqual("CN=" + subjectName, certificate.SubjectDN.ToString());
		}

		[Test]
		public void ExportPrivateKey_InPemFormat()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var keyPair = certificateGenerator.GetKeyPair();

			//Act
			var privateKeyText = certificateGenerator.ExportKeyPair(keyPair);

			//Assert
			Assert.True(privateKeyText.StartsWith("-----BEGIN RSA PRIVATE KEY-----\r\n"));
			Assert.True(privateKeyText.EndsWith("\r\n-----END RSA PRIVATE KEY-----\r\n"));
		}

		[Test]
		public void ImportKeyPair_InPemFormat()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();

			var privateKeyText = certificateGenerator.ExportKeyPair(caKeyPair);

			//Act
			var keyPair = certificateGenerator.ImportKeyPair(privateKeyText);

			//Assert
			Assert.AreEqual(caKeyPair.Private, keyPair.Private);
			Assert.AreEqual(caKeyPair.Public, keyPair.Public);
		}

		[Test]
		public void ExportCertificate_InPemFormat()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string caSubjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = caSubjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);

			//Act
			var certificateText = certificateGenerator.ExportCertificate(caCertificate);

			//Assert
			Assert.True(certificateText.StartsWith("-----BEGIN CERTIFICATE-----\r\n"));
			Assert.True(certificateText.EndsWith("\r\n-----END CERTIFICATE-----\r\n"));
		}

		[Test]
		public void ImportCertificate_InPemFormat()
		{
			// Arrange
			var certificateGenerator = new CertificateGenerator();
			var caKeyPair = certificateGenerator.GetKeyPair();
			const string caSubjectName = "Test CA";

			IDictionary caCertificateDetails = new Hashtable();
			caCertificateDetails[X509Name.CN] = caSubjectName;

			IList caCertificateDetailsOrder = new ArrayList();
			caCertificateDetailsOrder.Add(X509Name.CN);

			var caCertificate = certificateGenerator.GenerateCaCertificate(caKeyPair, caCertificateDetails, caCertificateDetailsOrder);
			var certificateText = certificateGenerator.ExportCertificate(caCertificate);

			//Act
			var certificate = certificateGenerator.ImportCertificate(certificateText);
			
			//Assert
			Assert.AreEqual(caCertificate.GetPublicKey(), certificate.GetPublicKey());
		}
	}
}
