using System.Security.Cryptography.X509Certificates;

namespace Provisioner.Shared.Payloads
{
    public class ConfigurationProfile
    {
        public IDictionary<Guid, X509Certificate2> RootCAs { get; } = new Dictionary<Guid, X509Certificate2>();
        public IDictionary<Guid, X509Certificate2Collection> Certificates { get; } = new Dictionary<Guid, X509Certificate2Collection>();
        public IList<WiFiConfig> WiFiConfigs { get; } = new List<WiFiConfig>();

        public bool IsValid()
        {
            if (WiFiConfigs.Any(d => !d.IsValid()))
            {
                return false;
            }

            foreach (var i in WiFiConfigs)
            {
                var enterpriseConfig = i.EnterpriseConfig;
                if (enterpriseConfig != default)
                {
                    foreach(var j in enterpriseConfig.TLSRootCertificates)
                    {
                        if (!RootCAs.ContainsKey(j))
                        {
                            return false;
                        }
                    }

                    if (enterpriseConfig.UserCertificate.HasValue)
                    {
                        if (!Certificates.ContainsKey(enterpriseConfig.UserCertificate.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
