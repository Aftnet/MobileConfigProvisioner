using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Provisioner.Shared.Extensions;
using Provisioner.Shared.Payloads;
using Provisioner.Shared.Services;

namespace Provisioner.Android.Services
{
    internal class WifiProvisioner : IWifiProvisioner
    {
        private const int RequestCode = 0x4844;
        private const string X509Str = "X.509";
        private const string PKCSExportPassword = "036acbc3";

        private Context mContext;

        private TaskCompletionSource<bool>? mTCS;
        private bool mOperationInProgress = false;

        public Activity? Activity { get; set; } = null;

        private static readonly Dictionary<WiFiEnterpriseConfig.EAPType, WifiEapMethod> EapMapping = new Dictionary<WiFiEnterpriseConfig.EAPType, WifiEapMethod>
        {
            { WiFiEnterpriseConfig.EAPType.EAP_PEAP, WifiEapMethod.Peap },
            { WiFiEnterpriseConfig.EAPType.EAP_TLS, WifiEapMethod.Tls },
            { WiFiEnterpriseConfig.EAPType.EAP_TTLS, WifiEapMethod.Ttls }
        };

        private static readonly Dictionary<WiFiEnterpriseConfig.TTLSInnerAuth, WifiPhase2Method> Phase2Mapping = new Dictionary<WiFiEnterpriseConfig.TTLSInnerAuth, WifiPhase2Method>
        {
            { WiFiEnterpriseConfig.TTLSInnerAuth.CHAP, WifiPhase2Method.Pap },
            { WiFiEnterpriseConfig.TTLSInnerAuth.MSCHAPv1, WifiPhase2Method.Mschap },
            { WiFiEnterpriseConfig.TTLSInnerAuth.MSCHAPv2, WifiPhase2Method.Mschapv2 },
            { WiFiEnterpriseConfig.TTLSInnerAuth.PAP, WifiPhase2Method.Pap },
        };

        private static readonly Dictionary<WiFiEnterpriseConfig.TLSVersion, WifiEnterpriseConfigTlsVersion> TlsVersionMapping = new Dictionary<WiFiEnterpriseConfig.TLSVersion, WifiEnterpriseConfigTlsVersion>
        {
            { WiFiEnterpriseConfig.TLSVersion.TLS_1_0, WifiEnterpriseConfigTlsVersion.V1_0 },
            { WiFiEnterpriseConfig.TLSVersion.TLS_1_1, WifiEnterpriseConfigTlsVersion.V1_1 },
            { WiFiEnterpriseConfig.TLSVersion.TLS_1_2, WifiEnterpriseConfigTlsVersion.V1_2 },
            { WiFiEnterpriseConfig.TLSVersion.TLS_1_3, WifiEnterpriseConfigTlsVersion.V1_3 }
        };

        public WifiProvisioner(Context? context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<bool> ProvisionAsync(ConfigurationProfile input)
        {
            if (mOperationInProgress || Activity == null)
            {
                return Task.FromResult(false);
            }

            var keychain = new global::Android.Security.KeyChain();

            var suggestions = new List<IParcelable>();
            foreach(var i in input.WiFiConfigs)
            {
                var builder = new WifiNetworkSuggestion.Builder();
                builder.SetSsid(i.SSID);

                if (i.Passphrase != null)
                {               
                    if (i.Mode == WiFiConfig.WpaMode.WPA3)
                    {
                        builder.SetWpa3Passphrase(i.Passphrase);
                    }
                    else
                    {
                        builder.SetWpa2Passphrase(i.Passphrase);
                    }
                }
                else if(i.EnterpriseConfig != null)
                {
                    var srcConfig = i.EnterpriseConfig;
                    var enterpriseConfig = new WifiEnterpriseConfig();
                    enterpriseConfig.EapMethod = EapMapping[srcConfig.AcceptedEAPTypes.First()];
                    enterpriseConfig.Phase2Method = Phase2Mapping[srcConfig.TTLSInnerAuthentication];
                    enterpriseConfig.AnonymousIdentity = srcConfig.OuterIdentity;
                    enterpriseConfig.MinimumTlsVersion = TlsVersionMapping[srcConfig.TLSMinimumVersion];

                    var trustedDomains = srcConfig.GetTLSTrustedServerDomains();
                    if (trustedDomains.Any())
                    {
                        enterpriseConfig.DomainSuffixMatch = trustedDomains.First();
                    }

                    if (srcConfig.TLSRootCertificates.Any())
                    {
                        var certFactory = Java.Security.Cert.CertificateFactory.GetInstance(X509Str)!;
                        var certs = srcConfig.TLSRootCertificates.Select(d =>
                        {
                            using (var stream = new MemoryStream(input.RootCAs[d].RawData))
                            {
                                var output = certFactory.GenerateCertificate(stream)!;
                                return (Java.Security.Cert.X509Certificate)output;
                            }
                        }).ToArray();
                        enterpriseConfig.SetCaCertificates(certs);
                    }
                    else
                    {
                        var certs = Array.Empty<Java.Security.Cert.X509Certificate>();
                        enterpriseConfig.SetCaCertificates(certs);
                    }

                    if (srcConfig.UserCertificate.HasValue)
                    {
                        var userCertChain = input.Certificates[srcConfig.UserCertificate.Value]!;
                        if (userCertChain.Any(d => d.HasPrivateKey))
                        {
                            var certFactory = Java.Security.Cert.CertificateFactory.GetInstance(X509Str)!;
                            var certs = userCertChain.Select(d =>
                            {
                                using (var stream = new MemoryStream(d.RawData))
                                {
                                    var output = certFactory.GenerateCertificate(stream)!;
                                    return (Java.Security.Cert.X509Certificate)output;
                                }
                            }).ToArray();

                            var userCert = userCertChain.First(d => d.HasPrivateKey);
                            using (var stream = new MemoryStream(userCert.ExportPkcs12(System.Security.Cryptography.X509Certificates.Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, PKCSExportPassword)))
                            {
                                var passCharArr = PKCSExportPassword.ToCharArray();
                                var keystore = Java.Security.KeyStore.GetInstance("pkcs12")!;
                                keystore.Load(stream, passCharArr);
                                var aliases = keystore.Aliases()!;
                                while (aliases.HasMoreElements)
                                {
                                    var alias = (String)aliases.NextElement()!;
                                    var certKey = keystore.GetKey(alias, passCharArr);
                                    var privKey = certKey.JavaCast<Java.Security.IPrivateKey>();

                                    enterpriseConfig.Identity = userCert.GetCommonName();
                                    enterpriseConfig.SetClientKeyEntryWithCertificateChain(privKey, certs);
                                }
                            }
                        }
                    }
                    else
                    {
                        enterpriseConfig.Identity = string.IsNullOrEmpty(srcConfig.UserName) ? null : srcConfig.UserName;
                        enterpriseConfig.Password = string.IsNullOrEmpty(srcConfig.UserPassword) ? null : srcConfig.UserPassword;
                    }

                    builder.SetWpa2EnterpriseConfig(enterpriseConfig);
                }

                suggestions.Add(builder.Build());
            }

            var bundle = new Bundle();
            bundle.PutParcelableArrayList(global::Android.Provider.Settings.ExtraWifiNetworkList, suggestions);
            var intent = new Intent(global::Android.Provider.Settings.ActionWifiAddNetworks);
            intent.PutExtras(bundle);

            mOperationInProgress = true;
            mTCS = new TaskCompletionSource<bool>();
            Activity.StartActivityForResult(intent, RequestCode);

            return mTCS.Task;
        }

        public void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != RequestCode)
            {
                return;
            }

            mTCS?.SetResult(resultCode == Result.Ok);
            mOperationInProgress = false;
        }
    }
}
