using System;
using Android.App;
using TestClientXamarin.Droid.Implementation;
using TestClientXamarin.Interface;
using Xamarin.Forms;
[assembly: Xamarin.Forms.Dependency(typeof(AndroidCloseAppImplementation))]
namespace TestClientXamarin.Droid.Implementation
{
   
        public class AndroidCloseAppImplementation : IAndroidCloseApp
        {
            public void CloseApp()
            {
                var activity = (Activity)Forms.Context;
                activity.FinishAffinity();

            }
        }
    
}
