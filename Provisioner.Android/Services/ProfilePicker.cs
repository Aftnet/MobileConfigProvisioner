using Android.Content;
using Provisioner.Shared.Services;

namespace Provisioner.Android.Services
{
    internal class ProfilePicker : IProfilePicker
    {
        private const int RequestCode = 0x0b3d;

        private Context mContext;

        private TaskCompletionSource<Stream?>? mTCS;
        private bool mOperationInProgress = false;

        public Activity? Activity { get; set; } = null;

        public ProfilePicker(Context? context)
        {
            mContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<Stream?> PickAsync()
        {
            if (mOperationInProgress || Activity == null)
            {
                return Task.FromResult(null as Stream);
            }

            var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType("*/*");
            intent.AddCategory(Intent.CategoryOpenable);
            intent.PutExtra(Intent.ExtraAllowMultiple, false);
            //var pickerIntent = Intent.CreateChooser(intent, "lol");

            mOperationInProgress = true;
            mTCS = new TaskCompletionSource<Stream?>();
            Activity.StartActivityForResult(intent, RequestCode);

            return mTCS.Task;
        }

        public void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != RequestCode)
            {
                return;
            }

            var uri = resultCode == Result.Ok ? data?.Data : null;
            var stream = uri != null ? mContext.ContentResolver!.OpenInputStream(uri) : null;
            mTCS?.SetResult(stream);
            mOperationInProgress = false;
        }
    }
}
