using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WallTec.CoreCom.Client;
using System.Threading.Tasks;
using WallTec.CoreCom.Example.SharedObjects;
using WallTec.CoreCom.Sheard;
using System.Collections.Generic;
using TestClientXamarin.View;

namespace TestClientXamarin
{
    public partial class App : Application
    {
        public static Services.ServiceCoreCom ServiceCoreCom = new Services.ServiceCoreCom(); 
        public App()
        {
            InitializeComponent();
            
            MainPage = new MainView();
        }

        protected async override void OnStart()
        {
           await ServiceCoreCom.SetupCoreComServer();

        }
        
        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
