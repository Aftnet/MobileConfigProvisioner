using Android.Content;
using Android.Views;
using AndroidX.AppCompat.Widget;
using MvvmCross;
using MvvmCross.Platforms.Android.Binding;
using MvvmCross.Platforms.Android.Views;
using Provisioner.Android.Services;
using Provisioner.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using static Android.Views.ViewGroup;

namespace Provisioner.Android.Views
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class Main : MvxActivity<Shared.ViewModels.Main>, View.IOnApplyWindowInsetsListener
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            var toolBar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar)!;
            SetSupportActionBar(toolBar);

            foreach (var i in new[] { Resource.Id.toolbar, Resource.Id.pickProfileButton })
            {
                var view = FindViewById<View>(i)!;
                view.SetOnApplyWindowInsetsListener(this);
            }

            using (var set = CreateBindingSet())
            {
                set.Bind(FindViewById<Button>(Resource.Id.pickProfileButton)).To(d => d.PickProfile);
            }
        }

        public WindowInsets OnApplyWindowInsets(View v, WindowInsets insets)
        {
            if (v.Id == Resource.Id.toolbar)
            {
                var insetMetrics = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.StatusBars());
                v.LayoutParameters!.Height += insetMetrics.Top;
                v.SetPadding(insetMetrics.Left, insetMetrics.Top, insetMetrics.Right, insetMetrics.Bottom);
            }
            else if(v.Id == Resource.Id.pickProfileButton)
            {
                var insetMetrics = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.NavigationBars());
                var layoutParams = v.LayoutParameters as LinearLayout.LayoutParams;
                layoutParams?.BottomMargin = insetMetrics.Bottom;
            }

            return WindowInsets.Consumed;
        }

        protected override void OnStart()
        {
            base.OnStart();

            var profilePicker = Mvx.IoCProvider?.GetSingleton<IProfilePicker>() as ProfilePicker ?? throw new ApplicationException();
            profilePicker.Activity = this;
            var wifiProvisioner = Mvx.IoCProvider?.GetSingleton<IWifiProvisioner>() as WifiProvisioner ?? throw new ApplicationException();
            wifiProvisioner.Activity = this;
        }

        protected override void OnStop()
        {
            var profilePicker = Mvx.IoCProvider?.GetSingleton<IProfilePicker>() as ProfilePicker ?? throw new ApplicationException();
            profilePicker.Activity = null;
            var wifiProvisioner = Mvx.IoCProvider?.GetSingleton<IWifiProvisioner>() as WifiProvisioner ?? throw new ApplicationException();
            wifiProvisioner.Activity = null;

            base.OnStop();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            var profilePicker = Mvx.IoCProvider?.GetSingleton<IProfilePicker>() as ProfilePicker ?? throw new ApplicationException();
            profilePicker.OnActivityResult(requestCode, resultCode, data);
            var wifiProvisioner = Mvx.IoCProvider?.GetSingleton<IWifiProvisioner>() as WifiProvisioner ?? throw new ApplicationException();
            wifiProvisioner.OnActivityResult(requestCode, resultCode, data);

            base.OnActivityResult(requestCode, resultCode, data);
        }
    }
}
