using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WallTec.CoreCom.Client;
using System.Threading.Tasks;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Sheard;
using System.Collections.Generic;
using TestClientXamarin.View;
using TestClientXamarin.Services.Dialog;

[assembly: ExportFont("Awesome_5_Free_Regular_400.otf", Alias = "labelIconBaseStyle")]
namespace TestClientXamarin
{
    public partial class App : Application
    {
        public static Services.ServiceCoreCom ServiceCoreCom = new Services.ServiceCoreCom(); 
        public App()
        {
            InitializeComponent();
            Xamarin.Forms.DependencyService.RegisterSingleton<IDialogService>(new DialogService());

            MainPage = new AppShell();
        }

        protected async override void OnStart()
        {
            DependencyService.RegisterSingleton<IDialogService>(new DialogService());
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
        public static void ConsoleWriteLineDebug(Exception ex)
        {
#if DEBUG
            Console.WriteLine(ex);
#endif

        }
        public static void ConsoleWriteLineDebug(string stringInfo)
        {
#if DEBUG
            Console.WriteLine(stringInfo);
#endif

        }
    }
}
