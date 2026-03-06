using Provisioner.Shared.Payloads;

namespace Provisioner.Shared.Services
{
    public interface IWifiProvisioner
    {
        public Task<bool> ProvisionAsync(ConfigurationProfile input);
    }
}
