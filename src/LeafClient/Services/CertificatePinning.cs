using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LeafClient.Services
{
    public static class CertificatePinning
    {
        public const bool EnforcePin = false;

        public static readonly string[] CdnSpkiPins = new[]
        {
            "VoDTX4iukqFp9JP7NGh0gMKeLWL4jBqNgjUJBLkjLUI=",
        };

        public static readonly string[] ApiSpkiPins = new[]
        {
            "cab6ZaWSqUlqGBlED/Kc8RI4JHFMCCzhdtYcMcM5adk=",
        };

        public static HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = ValidateCertificate;
            return handler;
        }

        private static readonly HashSet<string> _seenUnpinned = new();

        private static bool ValidateCertificate(
            HttpRequestMessage request,
            X509Certificate2? cert,
            X509Chain? chain,
            SslPolicyErrors policyErrors)
        {
            if (policyErrors != SslPolicyErrors.None) return false;
            if (cert is null) return false;

            string? host = request.RequestUri?.Host?.ToLowerInvariant();
            string[]? pins = host switch
            {
                "cdn.leafclient.com" => CdnSpkiPins,
                "api.leafclient.com" => ApiSpkiPins,
                _ => null,
            };
            if (pins is null) return true;

            string spkiHash;
            try
            {
                spkiHash = Convert.ToBase64String(SHA256.HashData(cert.GetPublicKey()));
            }
            catch (Exception ex)
            {
                LeafLog.Error("CertPin", $"Failed to hash SPKI for {host}: {ex.Message}");
                return !EnforcePin;
            }

            foreach (var pin in pins)
            {
                if (string.Equals(spkiHash, pin, StringComparison.Ordinal)) return true;
            }

            string seenKey = $"{host}|{spkiHash}";
            if (_seenUnpinned.Add(seenKey))
            {
                LeafLog.Info("CertPin", $"Unknown SPKI for {host}: {spkiHash} (expected one of: {string.Join(", ", pins)}). Mode={(EnforcePin ? "ENFORCE" : "MONITOR")}.");
            }
            return !EnforcePin;
        }
    }
}
