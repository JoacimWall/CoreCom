using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WallTec.CoreCom.Client;
using System.Threading.Tasks;
using WallTec.CoreCom.Example.Shared;
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
            
            MainPage = new MainTabbedView();
        }

        protected async override void OnStart()
        {
           ServiceCoreCom.SetupCoreComServer();
           await ServiceCoreCom.ConnectCoreComServer();
        }
        
        protected override void OnSleep()
        {
            ServiceCoreCom.DisconnectCoreComServer();
        }

        protected async override void OnResume()
        {
            await ServiceCoreCom.ConnectCoreComServer();
        }
    }
}
