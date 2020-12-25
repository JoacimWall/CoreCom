using Acr.UserDialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestClientXamarin.Services.Dialog;
using Xamarin.Forms;
namespace TestClientXamarin.Services.Dialog
{
    public class DialogService : IDialogService
    {
        public Task ShowAlertAsync(string message, string title, string buttonLabel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, buttonLabel); ;
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string destructive, params string[] buttons)
        {
            if (String.IsNullOrEmpty(title))
                title = "";

            buttons = buttons.Where(c => c != null).ToArray();

            var res = await Application.Current.MainPage.DisplayActionSheet(title, cancel, destructive, buttons);
            if (res == null)
                return "";

            return res;
        }
        public Task<string> ShowPromptAsync(string message, string title, string ok, string cancel)
        {
            return Application.Current.MainPage.DisplayPromptAsync(title, message, ok, cancel);
        }
        
        public Task<bool> ConfirmAsync(string message, string title, string ok, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, ok,cancel);          
        }
        public IProgressDialog GetProgress(string title)
        {
            var v = new ProgressDialogConfig();
            v.SetTitle(title);
            //v.MaskType = MaskType.Gradient;
            v.SetMaskType(Acr.UserDialogs.MaskType.Gradient); 
            return UserDialogs.Instance.Progress(v);
        }

        public void ShowToast(string message, ToastPosition toastPosition = ToastPosition.Top)
        {
            // Add top and botton space to iOS
            if (Device.RuntimePlatform == Device.iOS)
            {
                message = "\n" + message + "\n";
            }
            
            UserDialogs.Instance.Toast(new ToastConfig(message)
                       .SetBackgroundColor(TestClientXamarin.Helpers.StyleResources.Gray900Color())
                       .SetMessageTextColor(TestClientXamarin.Helpers.StyleResources.WhiteColor())
                       .SetDuration(TimeSpan.FromSeconds(6))
                       .SetPosition(toastPosition)
                   ) ;
        }

     

    }
}
