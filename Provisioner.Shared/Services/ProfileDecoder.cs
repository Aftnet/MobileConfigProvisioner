using Claunia.PropertyList;
using Provisioner.Shared.Payloads;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Provisioner.Shared.Services
{
    public class ProfileDecoder : IProfileDecoder
    {
        private const long MaxProfileSize = 1024 * 1024;

        private const string AcceptEAPTypesKey = "AcceptEAPTypes";
        private const string EAPClientConfigurationKey = "EAPClientConfiguration";
        private const string OuterIdentityKey = "OuterIdentity";
        private const string PayloadCertificateUUIDKey = "PayloadCertificateUUID";
        private const string PayloadCertificateAnchorUUIDKey = "PayloadCertificateAnchorUUID";
        private const string PayloadContentKey = "PayloadContent";
        private const string PayloadPasswordKey = "Password";
        private const string PayloadTypeKey = "PayloadType";
        private const string PayloadUUIDKey = "PayloadUUID";
        private const string TLSMinimumVersionKey = "TLSMinimumVersion";
        private const string TLSTrustedServerNamesKey = "TLSTrustedServerNames";
        private const string TTLSInnerAuthenticationKey = "TTLSInnerAuthentication";
        private const string UserNameKey = "UserName";
        private const string UserPasswordKey = "UserPassword";
        private const string WifiNetSSIDKey = "SSID_STR";
        private const string WifiNetPasswordKey = "Password";

        private static readonly Dictionary<int, WiFiEnterpriseConfig.EAPType> AcceptEAPTypesMapping = new Dictionary<int, WiFiEnterpriseConfig.EAPType>
        {
            { 25, WiFiEnterpriseConfig.EAPType.EAP_PEAP },
            { 13, WiFiEnterpriseConfig.EAPType.EAP_TLS },
            { 21, WiFiEnterpriseConfig.EAPType.EAP_TTLS }
        };

        private static readonly Dictionary<string, WiFiEnterpriseConfig.TLSVersion> TLSVersionMapping = new Dictionary<string, WiFiEnterpriseConfig.TLSVersion>
        {
            { "1.0", WiFiEnterpriseConfig.TLSVersion.TLS_1_0 },
            { "1.1", WiFiEnterpriseConfig.TLSVersion.TLS_1_1 },
            { "1.2", WiFiEnterpriseConfig.TLSVersion.TLS_1_2 },
            { "1.3", WiFiEnterpriseConfig.TLSVersion.TLS_1_3 }
        };

        private static readonly Dictionary<string, WiFiEnterpriseConfig.TTLSInnerAuth> TTLSInnerAuthMapping = new Dictionary<string, WiFiEnterpriseConfig.TTLSInnerAuth>
        {
            { "CHAP", WiFiEnterpriseConfig.TTLSInnerAuth.CHAP },
            { "MSCHAP", WiFiEnterpriseConfig.TTLSInnerAuth.MSCHAPv1 },
            { "MSCHAPv2", WiFiEnterpriseConfig.TTLSInnerAuth.MSCHAPv2 },
            { "PAP", WiFiEnterpriseConfig.TTLSInnerAuth.PAP },
        };

        public ConfigurationProfile Decode(Stream input)
        {
            if (input.Length > MaxProfileSize)
            {
                throw new ApplicationException("Maximum supported profile size exceeded");
            }

            var output = new ConfigurationProfile();
            var root = (NSDictionary)PropertyListParser.Parse(input);
            var payloadArray = (NSArray)root.ObjectForKey("PayloadContent");

            foreach (var i in GetDictionariesOfType(payloadArray, "com.apple.security.root"))
            {
                output.RootCAs.Add(GetDictionaryGuid(i), ParseCertificate(GetDictionaryBytes(i, PayloadContentKey)));
            }

            foreach (var i in GetDictionariesOfType(payloadArray, "com.apple.security.pem"))
            {
                output.Certificates.Add(GetDictionaryGuid(i), ParseCertificateBundlePEM(GetDictionaryBytes(i, PayloadContentKey)));
            }
            foreach (var i in GetDictionariesOfType(payloadArray, "com.apple.security.pkcs12"))
            {
                output.Certificates.Add(GetDictionaryGuid(i), ParseCertificateBundlePKCS12(GetDictionaryBytes(i, PayloadContentKey), GetDictionaryString(i, PayloadPasswordKey)));
            }

            var wiFiPayloads = GetDictionariesOfType(payloadArray, "com.apple.wifi.managed");
            foreach (var i in wiFiPayloads)
            {
                var ssid = i[WifiNetSSIDKey].ToString()!;
                if (i.ContainsKey(WifiNetPasswordKey))
                {
                    output.WiFiConfigs.Add(new WiFiConfig(WiFiConfig.WpaMode.WPA2, ssid, i[WifiNetPasswordKey].ToString()!));
                }
                else if (i.ContainsKey(EAPClientConfigurationKey))
                {
                    var enterpriseConfig = ParseEnterpriseConfig((NSDictionary)i[EAPClientConfigurationKey]);
                    if (i.TryGetValue(PayloadCertificateUUIDKey, out var certificateUUID))
                    {
                        enterpriseConfig.UserCertificate = new Guid(certificateUUID.ToString()!);
                    }

                    output.WiFiConfigs.Add(new WiFiConfig(WiFiConfig.WpaMode.WPA2, ssid, enterpriseConfig));
                }
                else
                {
                    throw new ApplicationException("Invalid wifi configuration");
                }
            }

            return output;
        }

        private List<NSDictionary> GetDictionariesOfType(NSArray input, string typeId)
        {
            var output = new List<NSDictionary>();
            foreach (var i in input)
            {
                var dict = (NSDictionary)i;
                if (dict[PayloadTypeKey].ToString() == typeId)
                {
                    output.Add(dict);
                }
            }

            return output;
        }

        private Guid GetDictionaryGuid(NSDictionary dictionary)
        {
            return new Guid(((NSString)dictionary[PayloadUUIDKey]).ToString());
        }

        private byte[] GetDictionaryBytes(NSDictionary dictionary, string key)
        {
            return ((NSData)dictionary[key]).Bytes;
        }

        private string GetDictionaryString(NSDictionary dictionary, string key)
        {
            return ((NSString)dictionary[key]).ToString();
        }

        private X509Certificate2 ParseCertificate(ReadOnlySpan<byte> data)
        {
            return X509CertificateLoader.LoadCertificate(data);
        }

        private X509Certificate2Collection ParseCertificateBundlePEM(ReadOnlySpan<byte> data)
        {
            var certChainString = Encoding.Default.GetString(data);
            var searchSpan = certChainString.AsSpan();
            var privKeySpan = ReadOnlySpan<char>.Empty;
            while (PemEncoding.TryFind(searchSpan, out var pemFields))
            {
                var label = searchSpan[pemFields.Label.Start..pemFields.Label.End];
                if (label.Contains("PRIVATE KEY", StringComparison.Ordinal))
                {
                    privKeySpan = searchSpan[pemFields.Location];

                    break;
                }
                searchSpan = searchSpan[pemFields.Location.End.Value..];
            }

            var certs = new X509Certificate2Collection();
            searchSpan = certChainString.AsSpan();
            while (PemEncoding.TryFind(searchSpan, out var pemFields))
            {
                var label = searchSpan[pemFields.Label.Start..pemFields.Label.End];
                if (label.Contains("CERTIFICATE", StringComparison.Ordinal))
                {
                    if (!privKeySpan.IsEmpty)
                    {
                        certs.Add(X509Certificate2.CreateFromPem(searchSpan[pemFields.Location], privKeySpan));
                        privKeySpan = ReadOnlySpan<char>.Empty;
                    }
                    else
                    {
                        certs.Add(X509Certificate2.CreateFromPem(searchSpan[pemFields.Location]));
                    }
                }
                searchSpan = searchSpan[pemFields.Location.End.Value..];
            }

            return certs;
        }

        private X509Certificate2Collection ParseCertificateBundlePKCS12(ReadOnlySpan<byte> data, string? password)
        {
            var certs = X509CertificateLoader.LoadPkcs12Collection(data, password);
            if (certs == null || certs.Count == 0)
            {
                throw new ApplicationException("No Certificates found in pkcs12 data");
            }

            certs = new X509Certificate2Collection(certs.OrderBy(d => d.HasPrivateKey, Comparer<bool>.Create((d,e) =>
            {
                if (d == e) return 0;
                else if (d == true) return -1;
                else return 1;
            })).ToArray());
            return certs;
        }

        private WiFiEnterpriseConfig ParseEnterpriseConfig(NSDictionary input)
        {
            var output = new WiFiEnterpriseConfig();

            if (input.TryGetValue(AcceptEAPTypesKey, out var acceptEAPTypes))
            {
                foreach (var i in (NSArray)acceptEAPTypes)
                {
                    if (AcceptEAPTypesMapping.TryGetValue((int)(NSNumber)i, out var value))
                    {
                        output.AcceptedEAPTypes.Add(value);
                    }
                }
            }

            if (input.TryGetValue(OuterIdentityKey, out var outerIdentity))
            {
                output.OuterIdentity = outerIdentity.ToString()!;
            }

            if (input.TryGetValue(PayloadCertificateAnchorUUIDKey, out var payloadCertificateAnchorUUIDArray))
            {
                foreach (var i in (NSArray)payloadCertificateAnchorUUIDArray)
                {
                    output.TLSRootCertificates.Add(new Guid(i.ToString()!));
                }
            }

            if (input.TryGetValue(TLSMinimumVersionKey, out var tLSMinimumVersion))
            {
                if(TLSVersionMapping.TryGetValue(tLSMinimumVersion.ToString()!, out var mappedTLSVersion))
                {
                    output.TLSMinimumVersion = mappedTLSVersion;
                }
            }

            if (input.TryGetValue(TLSTrustedServerNamesKey, out var tLSTrustedServerNamesArray))
            {
                foreach (var i in (NSArray)tLSTrustedServerNamesArray)
                {
                    output.TLSTrustedServerNames.Add(i.ToString()!);
                }
            }

            if (input.TryGetValue(TTLSInnerAuthenticationKey, out var tTLSInnerAuthentication))
            {
                if (TTLSInnerAuthMapping.TryGetValue(tTLSInnerAuthentication.ToString()!, out var mappedTTLSInnerAuthentication))
                {
                    output.TTLSInnerAuthentication = mappedTTLSInnerAuthentication;
                }
            }

            if (input.TryGetValue(UserNameKey, out var userName))
            {
                output.UserName = userName.ToString()!;
            }

            if (input.TryGetValue(UserPasswordKey, out var userPassword))
            {
                output.UserPassword = userPassword.ToString()!;
            }

            return output;
        }
    }
}
