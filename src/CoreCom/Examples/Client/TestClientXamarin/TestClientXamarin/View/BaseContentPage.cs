using System;
using TestClientXamarin.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;

namespace TestClientXamarin.View
{

    public class BaseContentPage : ContentPage
    {
        public BaseContentPage()
        {
            Xamarin.Forms.NavigationPage.SetBackButtonTitle(this, "");
            Xamarin.Forms.NavigationPage.SetHasBackButton(this, false);


            On<iOS>().SetUseSafeArea(true);

        }

        protected override bool OnBackButtonPressed()
        {
            if (BindingContext != null)
                (BindingContext as IContentPageLifeCycleAsync)?.OnBackButtonPressedAsync();

            return true;
            //return base.OnBackButtonPressed();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
         
            // //Title color
            // App.Current.MainPage.SetValue(Xamarin.Forms.NavigationPage.BarTextColorProperty, Color.White);
            if (BindingContext != null)
                await (BindingContext as IContentPageLifeCycleAsync)?.OnAppearingAsync();

        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext != null)
                await (BindingContext as IContentPageLifeCycleAsync)?.OnDisappearingAsync();

          

        }
    }
}
