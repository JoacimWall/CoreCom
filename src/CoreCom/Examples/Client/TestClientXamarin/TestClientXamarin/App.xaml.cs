using System;
using Xamarin.Forms;
using TestClientXamarin.Services.Dialog;
using TestClientXamarin.Repository;

[assembly: ExportFont("Awesome_5_Free_Regular_400.otf", Alias = "labelIconBaseStyle")]
namespace TestClientXamarin
{
    public partial class App : Application
    {

        public static Services.ServiceCoreCom ServiceCoreCom = new Services.ServiceCoreCom();
        public static InMemoryData InMemoryData = new InMemoryData();

        public App()
        {
            InitializeComponent();
            DependencyService.RegisterSingleton<IDialogService>(new DialogService());
            //This so we capture logenvent from start not jsut efter first view of the logview
            DependencyService.RegisterSingleton(new ViewModel.LogViewModel());

            MainPage = new AppShell();
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
