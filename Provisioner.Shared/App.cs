using MvvmCross;
using MvvmCross.ViewModels;
using Provisioner.Shared.Services;
using System.Diagnostics.CodeAnalysis;

namespace Provisioner.Shared;

[RequiresUnreferencedCode("")]
public class App : MvxApplication
{
    public override void Initialize()
    {
        Mvx.IoCProvider!.RegisterType<IProfileDecoder, ProfileDecoder>();

        RegisterAppStart<ViewModels.Main>();
    }
}