namespace Provisioner.Shared.Services
{
    public interface IProfilePicker
    {
        Task<Stream?> PickAsync();
    }
}
