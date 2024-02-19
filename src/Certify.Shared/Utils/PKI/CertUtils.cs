using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.Oiw;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Certify.Shared.Core.Utils.PKI
{
    /// <summary>
    /// Terminology from https://en.wikipedia.org/wiki/Chain_of_trust
    /// </summary>
    [Flags]
    public enum ExportFlags
    {
        EndEntityCertificate = 1,
        IntermediateCertificates = 4,
        RootCertificate = 6,
        PrivateKey = 8
    }

    public static class CertUtils
    {
        /// <summary>
        /// Get PEM encoded cert bytes (intermediates only or full chain) from PFX bytes
        /// </summary>
        /// <param name="pfxData"></param>
        /// <param name="pwd">private key password</param>
        /// <param name="flags">Flags for component types to export</param>
        /// <returns></returns>
        public static byte[] GetCertComponentsAsPEMBytes(byte[] pfxData, string pwd, ExportFlags flags)
        {
            var pem = GetCertComponentsAsPEMString(pfxData, pwd, flags);
            return System.Text.Encoding.ASCII.GetBytes(pem);
        }

        public static string GetCertComponentsAsPEMString(byte[] pfxData, string pwd, ExportFlags flags)
        {
            // See also https://www.digicert.com/ssl-support/pem-ssl-creation.htm

            var cert = new X509Certificate2(pfxData, pwd);
            var chain = new X509Chain();
            chain.Build(cert);

            using (var writer = new StringWriter())
            {
                var certParser = new X509CertificateParser();
                var pemWriter = new PemWriter(writer);

                //output in order of private key, primary cert, intermediates, root

                if (flags.HasFlag(ExportFlags.PrivateKey))
                {
                    var key = GetCertKeyPem(pfxData, pwd);
                    writer.Write(key);
                }

                var i = 0;
                foreach (var c in chain.ChainElements)
                {
                    if (i == 0 && flags.HasFlag(ExportFlags.EndEntityCertificate))
                    {
                        // first cert is end entity cert (primary certificate)
                        var o = c.Certificate.Export(X509ContentType.Cert);
                        pemWriter.WriteObject(certParser.ReadCertificate(o));

                    }
                    else if (i == chain.ChainElements.Count - 1 && flags.HasFlag(ExportFlags.RootCertificate))
                    {
                        // last cert is root ca public cert
                        var o = c.Certificate.Export(X509ContentType.Cert);
                        pemWriter.WriteObject(certParser.ReadCertificate(o));
                    }
                    else if (i != 0 && (i != chain.ChainElements.Count - 1) && flags.HasFlag(ExportFlags.IntermediateCertificates))
                    {
                        // intermediate cert(s), if any, not including end entity and root
                        var o = c.Certificate.Export(X509ContentType.Cert);
                        pemWriter.WriteObject(certParser.ReadCertificate(o));
                    }

                    i++;
                }

                writer.Flush();

                return writer.ToString();
            }
        }

        /// <summary>
        /// Get PEM encoded private key bytes from PFX bytes
        /// </summary>
        /// <param name="pfxData"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static string GetCertKeyPem(byte[] pfxData, string pwd)
        {
            var pkcsStore = new Pkcs12StoreBuilder().Build();
            pkcsStore.Load(new MemoryStream(pfxData), pwd.ToCharArray());

            var keyAlias = pkcsStore.Aliases
                                    .OfType<string>()
                                    .Where(a => pkcsStore.IsKeyEntry(a))
                                    .FirstOrDefault();

            var key = pkcsStore.GetKey(keyAlias).Key;

            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(key);
                writer.Flush();
                return writer.ToString();
            }
        }

        /// <summary>
        /// For a given PFX, calculate the Base64Url encoded SHA256 CertID based on public key and serial (see OCSP CertID)
        /// </summary>
        /// <param name="pfxData"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static string GetCertIdBase64(byte[] pfxData, string pwd)
        {
            var certPfx = new X509Certificate2(pfxData, pwd);

            var cert = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(certPfx.GetRawCertData());
            
           
            var certId = new CertificateID(CertificateID.DigestSha1, cert, cert.SerialNumber);

            return Certify.Management.Util.ToUrlSafeBase64String(certId.ToAsn1Object().GetDerEncoded());
        }
    }
}
