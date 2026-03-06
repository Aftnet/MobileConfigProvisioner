using Android.Content;
using Android.Views;
using MvvmCross;
using MvvmCross.Platforms.Android.Views;
using Provisioner.Android.Services;
using Provisioner.Shared.Services;
using System.Diagnostics.CodeAnalysis;

namespace Provisioner.Android.Views
{
    [Activity(Label = "@string/header_view_error")]
    [RequiresUnreferencedCode("")]
    public class Error : MvxActivity<Shared.ViewModels.Error>, View.IOnApplyWindowInsetsListener
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_error);

            var toolBar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar)!;
            toolBar.SetOnApplyWindowInsetsListener(this);
            SetSupportActionBar(toolBar);

            using (var set = CreateBindingSet())
            {
                set.Bind(FindViewById<TextView>(Resource.Id.titleTextView)).To(d => d.Title);
                set.Bind(FindViewById<TextView>(Resource.Id.messageTextView)).To(d => d.Message);
            }
        }

        public WindowInsets OnApplyWindowInsets(View v, WindowInsets insets)
        {
            var insetMetrics = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.StatusBars());
            v.LayoutParameters!.Height += insetMetrics.Top;
            v.SetPadding(insetMetrics.Left, insetMetrics.Top, insetMetrics.Right, insetMetrics.Bottom);
            return WindowInsets.Consumed;
        }
    }
}
