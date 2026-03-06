using System;

namespace Provisioner.Shared.Payloads
{
    public class WiFiEnterpriseConfig
    {
        public enum EAPType { EAP_PEAP, EAP_TLS, EAP_TTLS };
        public enum TTLSInnerAuth { CHAP, MSCHAPv1, MSCHAPv2, PAP };
        public enum TLSVersion { TLS_1_0, TLS_1_1, TLS_1_2, TLS_1_3 }

        public ISet<EAPType> AcceptedEAPTypes { get; } = new HashSet<EAPType>();
        public TTLSInnerAuth TTLSInnerAuthentication { get; set; } = TTLSInnerAuth.PAP;
        public string OuterIdentity { get; set; } = string.Empty;
        public TLSVersion TLSMinimumVersion { get; set; } = TLSVersion.TLS_1_3;
        public ISet<string> TLSTrustedServerNames { get; } = new HashSet<string>();
        public ISet<Guid> TLSRootCertificates { get; } = new HashSet<Guid>();
        public string? UserName { get; set; } = default;
        public string? UserPassword { get; set; } = default;
        public Guid? UserCertificate { get; set; } = default;

        public bool IsValid()
        {
            if (UserCertificate == default)
            {
                if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(UserPassword))
                {
                    return false;
                }
            }

            if (!AcceptedEAPTypes.Any())
            {
                return false;
            }

            if (!TLSTrustedServerNames.Any() && !TLSRootCertificates.Any())
            {
                return false;
            }

            return true;
        }

        public ISet<string> GetTLSTrustedServerDomains()
        {
            string HostnameToDomain(string hostname)
            {
                const char delimiter = '.';
                if (hostname.Last() == delimiter)
                {
                    hostname = hostname.Substring(0, hostname.Length - 1);
                }

                if (hostname.Count(delimiter) > 1)
                {
                    hostname = hostname.Substring(hostname.IndexOf(delimiter) + 1);
                }

                return hostname;
            }

            return new HashSet<string>(TLSTrustedServerNames.Select(HostnameToDomain));
        }
    }
}
