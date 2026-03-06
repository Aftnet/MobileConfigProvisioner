using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android.Core;
using Provisioner.Android.Services;
using Provisioner.Shared.Services;
using Serilog;
using Serilog.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Provisioner.Android;

public class Setup : MvxAndroidSetup<Shared.App>
{
    [RequiresUnreferencedCode("")]
    protected override void InitializeLastChance(IMvxIoCProvider iocProvider)
    {
        base.InitializeLastChance(iocProvider);
        iocProvider.RegisterSingleton<IProfilePicker>(new ProfilePicker(ApplicationContext));
        iocProvider.RegisterSingleton<IWifiProvisioner>(new WifiProvisioner(ApplicationContext));
        iocProvider.RegisterType<MvxNavigationService>();
    }

    protected override ILoggerProvider CreateLogProvider()
    {
        return new SerilogLoggerProvider();
    }

    protected override ILoggerFactory CreateLogFactory()
    {
        // serilog configuration
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            // add more sinks here
            .CreateLogger();

        return new SerilogLoggerFactory();
    }
}