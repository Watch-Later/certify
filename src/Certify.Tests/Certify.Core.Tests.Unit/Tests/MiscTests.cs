﻿using System;
using System.Threading.Tasks;
using Certify.Management;
using Certify.Models.API;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Certify.Core.Tests.Unit
{
    [TestClass]
    public class MiscTests
    {
        [TestMethod, Description("Test null/blank coalesce of string")]
        public void TestNullOrBlankCoalesce()
        {
            string testValue = null;

            var result = testValue.WithDefault("ok");
            Assert.AreEqual(result, "ok");

            testValue = "test";
            result = testValue.WithDefault("ok");
            Assert.AreEqual(result, "test");

            var ca = new Models.CertificateAuthority();
            ca.Description = null;
            result = ca.Description.WithDefault("default");
            Assert.AreEqual(result, "default");

            ca = null;
            result = ca?.Description.WithDefault("default");
            Assert.AreEqual(result, null);
        }

        [TestMethod, Description("Test log parser using array of strings")]
        public void TestLogParser()
        {
            var testLog = new string[]
            {
                "2023-06-14 13:00:30.480 +08:00 [WRN] ARI Update Renewal Info Failed[MGAwDQYJYIZIAWUDBAIBBQAEIDfbgj - 5Rkkn0NG7u0eFv_M1omHdEwY_mIQn6QxbuJ68BCA9ROYZMeqCkxyMzaMePORi17Gc9xSbp8XkoE1Ub0IPrwILBm8t23CUKQnarrc] Fail to load resource from 'https://acme-staging-v02.api.letsencrypt.org/draft-ietf-acme-ari-01/renewalInfo/'." ,
                "urn:ietf:params:acme: error: malformed: Certificate not found" ,
                "2023-06-14 13:01:11.139 +08:00 [INF] Performing Certificate Request: SporkDemo[zerossl][2390d803 - e036 - 4bf5 - 8fa5 - 590497392c35: 7]"
            };

            var items = LogParser.Parse(testLog);

            Assert.AreEqual(2, items.Length);

            Assert.AreEqual("WRN", items[0].LogLevel);
            Assert.AreEqual("INF", items[1].LogLevel);

        }

        [TestMethod, Description("Test ntp check")]
        public async Task TestNtp()
        {
            var check = await Certify.Management.Util.CheckTimeServer();

            var timeDiff = check - DateTimeOffset.UtcNow;

            if (Math.Abs(timeDiff.Value.TotalSeconds) > 50)
            {
                Assert.Fail("NTP Time Difference Failed");
            }
        }

        [TestMethod, Description("Test self signed cert")]
        public void TestSelfSignedCertCreate()
        {

            var cert = CertificateManager.GenerateSelfSignedCertificate("test.com", new DateTime(1934, 01, 01), new DateTime(1934, 03, 01), suffix: "[Certify](test)");
            Assert.IsNotNull(cert);
        }

        [TestMethod, Description("Test self signed cert storage")]
        public void TestSelfSignedCertCreateAndStore()
        {

            var cert = CertificateManager.GenerateSelfSignedCertificate("test.com", new DateTime(1934, 01, 01), new DateTime(1934, 03, 01), suffix: "[Certify](test)");
            Assert.IsNotNull(cert);

            CertificateManager.StoreCertificate(cert, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);

            var storedCert = CertificateManager.GetCertificateByThumbprint(cert.Thumbprint, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);
            Assert.IsNotNull(storedCert);

            CertificateManager.RemoveCertificate(storedCert, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);
        }

        [TestMethod, Description("Test localhost cert")]
        public void TestSelfSignedLocalhostCertCreateAndStore()
        {

            var cert = CertificateManager.GenerateSelfSignedCertificate("localhost", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), suffix: "[Certify](test)");
            Assert.IsNotNull(cert);

            CertificateManager.StoreCertificate(cert, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);

            var storedCert = CertificateManager.GetCertificateByThumbprint(cert.Thumbprint, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);
            Assert.IsNotNull(storedCert);

            CertificateManager.RemoveCertificate(storedCert, Certify.Management.CertificateManager.DEFAULT_STORE_NAME);
        }
    }
}
