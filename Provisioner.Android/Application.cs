using Android.Runtime;
using MvvmCross.Platforms.Android.Views;
using System.Diagnostics.CodeAnalysis;

namespace Provisioner.Android;

[Application]
public class App : MvxAndroidApplication<Setup, Shared.App>
{
    [RequiresUnreferencedCode("")]
    public App(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}