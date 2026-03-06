using Provisioner.Shared.Payloads;
using System.Reflection;
using Xunit;

namespace Provisioner.Test.Services
{
    public class ProfileDecoder
    {
        readonly Shared.Services.ProfileDecoder mDecoder = new Shared.Services.ProfileDecoder();

        [Fact]
        public void Test()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var configProfile = default(ConfigurationProfile);
            using var stream = File.OpenRead(@"Data\Test.mobileconfig");
            {
                configProfile = mDecoder.Decode(stream);
            }

            Assert.True(configProfile.IsValid());
        }
    }
}
