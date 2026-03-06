namespace Provisioner.Shared.Payloads
{
    public class WiFiConfig
    {
        public enum WpaMode { WPA2, WPA3 };
        public WpaMode Mode { get; } = WpaMode.WPA2;
        public string SSID { get; } = string.Empty;
        public string? Passphrase { get; }
        public WiFiEnterpriseConfig? EnterpriseConfig { get; }

        public WiFiConfig(WpaMode mode, string ssid, string passphrase) : this(mode, ssid, passphrase, default)
        {
        }

        public WiFiConfig(WpaMode mode, string ssid, WiFiEnterpriseConfig enterpriseConfig) : this(mode, ssid, default, enterpriseConfig)
        {
        }

        private WiFiConfig(WpaMode mode, string ssid, string? passphrase, WiFiEnterpriseConfig? enterpriseConfig)
        {
            Mode = mode;
            SSID = string.IsNullOrEmpty(ssid) ? throw new ArgumentException(nameof(ssid)) : ssid;
            if (string.IsNullOrEmpty(passphrase) && enterpriseConfig == null)
            {
                throw new Exception("Passphrase or enterprise config required");
            }

            Passphrase = passphrase;
            EnterpriseConfig = enterpriseConfig;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(SSID))
            {
                return false;
            }

            if (string.IsNullOrEmpty(Passphrase) && EnterpriseConfig == default)
            {
                return false;
            }

            if (EnterpriseConfig != null)
            {
                return EnterpriseConfig.IsValid();
            }

            return true;
        }
    }
}
