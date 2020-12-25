using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TestClientXamarin.Interface;
using TestClientXamarin.Interfaces;
using TestClientXamarin.Services.Dialog;
using Xamarin.Forms;
namespace TestClientXamarin.ViewModel
{

    public class BaseViewModel : MvvmHelpers.BaseViewModel, IContentPageLifeCycleAsync // ExtendedBindableObject
    {
        protected readonly IDialogService DialogService;
        //protected readonly INavigationService NavigationService;
        public BaseViewModel()
        {
            DialogService = new DialogService();
            //NavigationService = ViewModelLocator.Resolve<INavigationService>();
            FirstTimeAppearing = true;


        }
        public virtual ICommand NavBackCommand => new Command(async () => await NavBackAsync());

        private bool _showNavbarBackbutton;
        public bool ShowNavbarBackbutton
        {
            get { return _showNavbarBackbutton; }
            set { SetProperty(ref _showNavbarBackbutton, value); }
        }
        public bool FirstTimeAppearing { get; set; }
        public virtual Task InitializeAsync(object navigationData) => Task.FromResult(false);



        public async virtual Task OnBackButtonPressedAsync()
        {

            await NavBackAsync();

        }
        //Android
        public async virtual Task CleanUpAsync()
        {

            // await NavBackAsync();

        }

        public virtual Task OnDisappearingAsync()
        {

            return Task.FromResult(true);
        }

        public virtual Task OnAppearingAsync()
        {



            FirstTimeAppearing = false;
            return Task.FromResult(true);
        }

        public bool PromptToConfirmExit
        {
            get
            {
                bool promptToConfirmExit = false;
                if (App.Current.MainPage is ContentPage)
                {
                    return true;
                }
                else if (App.Current.MainPage is Xamarin.Forms.MasterDetailPage masterDetailPage
                    && masterDetailPage.Detail is NavigationPage detailNavigationPage)
                {
                    return detailNavigationPage.Navigation.NavigationStack.Count <= 1;
                }
                else if (App.Current.MainPage is NavigationPage mainPage)
                {
                    if (mainPage.CurrentPage is TabbedPage tabbedPage
                        && tabbedPage.CurrentPage is NavigationPage navigationPage)
                    {
                        return navigationPage.Navigation.NavigationStack.Count <= 1;
                    }
                    else
                    {
                        return mainPage.Navigation.NavigationStack.Count <= 1;
                    }
                }
                else if (App.Current.MainPage is TabbedPage tabbedPage && tabbedPage.CurrentPage is NavigationPage navigationPage)
                {
                    return navigationPage.Navigation.NavigationStack.Count <= 1;
                }
                else if (App.Current.MainPage is AppShell) //shellPage && shellPage.CurrentPage is NavigationPage navigationPage
                {
                    return true; //navigationPage.Navigation.NavigationStack.Count <= 1
                }
                return promptToConfirmExit;
            }
        }

        private async Task NavBackAsync()
        {
            if (PromptToConfirmExit)
            {
                if (_isBackPressed)
                {
                    if (Device.RuntimePlatform == Device.Android)
                        DependencyService.Get<IAndroidCloseApp>().CloseApp();
                    //FinishAffinity(); // inform Android that we are done with the activity
                    return;
                }

                _isBackPressed = true;
                DialogService.ShowToast("Press back again to Exit", Acr.UserDialogs.ToastPosition.Bottom);
                // Toast.MakeText(this, AppResources.Press_back_Again_To_Exit, ToastLength.Short).Show();

                // Disable back to exit after 2 seconds.
                Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                {
                    _isBackPressed = false;
                    return false; // True = Repeat again, False = Stop the timer
                    });
                return;
            }

            if (ShowNavbarBackbutton)
            {
                //   await NavigationService.RemovePageAsync();
            }
        }

        public virtual void OnDisappearing()
        {

        }
        public virtual void OnAppearing()
        {

        }

        bool _isBackPressed = false;


    }

}
