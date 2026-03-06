using Provisioner.Shared.Payloads;

namespace Provisioner.Shared.Services
{
    public interface IProfileDecoder
    {
        ConfigurationProfile Decode(Stream input);
    }
}