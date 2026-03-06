using MvvmCross.Commands;
using MvvmCross.Navigation;
using Provisioner.Shared.Payloads;
using Provisioner.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Provisioner.Shared.ViewModels
{
    [RequiresUnreferencedCode("")]
    public class Main : MvvmCross.ViewModels.MvxViewModel
    {
        private readonly IMvxNavigationService mNavigator;
        private readonly IProfileDecoder mProfileDecoder;
        private readonly IProfilePicker mProfilePicker;
        private readonly IWifiProvisioner mWifiProvisioner;

        public IMvxCommand PickProfile { get; }

        public Main(IMvxNavigationService navigator, IProfileDecoder profileDecoder, IProfilePicker profilePicker, IWifiProvisioner wifiProvisioner)
        {
            mNavigator = navigator;
            mProfileDecoder = profileDecoder;
            mProfilePicker = profilePicker;
            mWifiProvisioner = wifiProvisioner;

            PickProfile = new MvxCommand(() => { var task = OnPickProfile(); });
        }

        public async Task OnPickProfile()
        {
            var profileStream = await mProfilePicker.PickAsync();
            //var profileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Provisioner.Shared.Test.mobileconfig");
            if (profileStream == null)
            {
                return;
            }

            var configProfile = default(ConfigurationProfile);
            using (profileStream)
            {
                try
                {
                    configProfile = mProfileDecoder.Decode(profileStream);
                }
                catch (Exception ex)
                {
                    var task = mNavigator.Navigate<Error, (string, string)>(("Invalid configuration profile", ex.Message));
                    return;
                }
            }

            if (configProfile != null)
            {
                try
                {
                    await mWifiProvisioner.ProvisionAsync(configProfile);
                }
                catch (Exception ex)
                {
                    var task = mNavigator.Navigate<Error, (string, string)>(("Error provisioning Wi-Fi", ex.Message));
                    return;
                }
            }
        }
    }
}
