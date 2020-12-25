using Acr.UserDialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestClientXamarin.Services.Dialog
{
    public interface IDialogService
    {
        Task ShowAlertAsync(string message, string title, string buttonLabel);
        Task<string> ShowPromptAsync(string message, string title, string ok, string cancel);
        Task<string> ShowActionSheetAsync(string title, string cancel, string destructive, params String[] buttons);

        Task<bool> ConfirmAsync(string message, string title, string ok, string cancel);
        IProgressDialog GetProgress(string title);
        void ShowToast(string message, ToastPosition toastPosition = ToastPosition.Top);
    //    void ShowToastWithAction(string message, string id);
    }
}
