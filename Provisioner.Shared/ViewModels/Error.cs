using System.Diagnostics.CodeAnalysis;

namespace Provisioner.Shared.ViewModels
{
    [RequiresUnreferencedCode("")]
    public class Error : MvvmCross.ViewModels.MvxViewModel<(string, string)>
    {
        private string title = string.Empty;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string message = string.Empty;
        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public override void Prepare((string, string) parameter)
        {
            Title = parameter.Item1;
            Message = parameter.Item2;
        }
    }
}
