using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using TestClientXamarin.Services;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Sheard.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TestClientXamarin.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            //Setup events
            App.ServiceCoreCom.CoreComClient.Register<List<Project>>(CoreComSignatures.ResponseAllProjects, GetAllProjects);
            App.ServiceCoreCom.CoreComClient.Register<Result<Project>>(CoreComSignatures.AddProject, GetAddedProject);
        }

        public ICommand CheckQueueCommand => new Command(async () => await CheckQueueCommandAsync());
        public ICommand ConnectToServerCommand => new Command(async () => await ConnectCommandAsync());

        private async Task ConnectCommandAsync()
        {
            ServiceCoreCom.LatestRpcException = string.Empty;
            if (App.ServiceCoreCom.ConnectionStatus != WallTec.CoreCom.Sheard.ConnectionStatusEnum.Connected)
            {
                await App.ServiceCoreCom.ConnectCoreComServer();
            }
            else
            {
                string username = (Device.RuntimePlatform == Device.Android ? "demoDroid" : "demoIos"); //simulate diffrent user
                await App.ServiceCoreCom.Authenticate(username, "1234");
            }
        }

        public ICommand GetProjectsCommand => new Command(async () => await GetProjectsAsync());
        public ICommand AddProjectsCommand => new Command(async () => await AddProjectsAsync());
        public ICommand PickImageCommand => new Command(async () => await PickImageAsync());

       

        private string _base64;
        private async Task PickImageAsync()
        {
            try
            {
                var photo = await MediaPicker.PickPhotoAsync();
                if (photo == null)
                    return;
                Stream stream = await photo.OpenReadAsync();
               
                if (stream != null)
                {
                    using (MemoryStream memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        byte[] bytes = memory.ToArray();
                        ProjectImageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
                        _base64 = Convert.ToBase64String(bytes);
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"CapturePhotoAsync THREW: {ex.Message}");
            }
        }
        private ImageSource _projectImageSource;

        public ImageSource ProjectImageSource
        {
            get { return _projectImageSource; }
            set { SetProperty(ref _projectImageSource, value); }
        }
        //public static byte[] ReadFully(Stream input)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        input.CopyTo(ms);
        //        return ms.ToArray();
        //    }
        //}
        private async Task AddProjectsAsync()
        {
            App.ServiceCoreCom.SendAuthAsync(new Project { Name = AddProjectName, Description = "project added from client",Base64Image = _base64 }, CoreComSignatures.AddProject);
            
        }
        private async Task CheckQueueCommandAsync()
        {
            App.ServiceCoreCom.CheckServerQueue();
        }
        private async Task GetProjectsAsync()
        {
            App.ServiceCoreCom.SendAsync(CoreComSignatures.RequestAllProjects);
        }

        private ObservableCollection<Project> _projects;
        public ObservableCollection<Project> Projects
        {
            get { return _projects; }
            set { SetProperty(ref _projects, value); }  
        }
        private string _addProjectName;
        public string AddProjectName
        {
            get { return _addProjectName; }
            set { SetProperty(ref _addProjectName, value); }
        }
        
        public ServiceCoreCom ServiceCoreCom
        {
            get { return App.ServiceCoreCom; }
           
        }
        private async void GetAllProjects(List<Project> projects)
        {
            App.InMemoryData.Projects = projects;
           
                Projects = new ObservableCollection<Project>(projects);
           

        }
        private async void GetAddedProject(Result<Project> result)
        {
            //the server wrapps in a result object to be able to return errors/validation 
            if (!result.WasSuccessful)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Code to run on the main thread
                    await DialogService.ShowAlertAsync(result.UserMessage, "CoreCom", "Ok");
                });
               
                return;
            }

          App.InMemoryData.Projects.Add(result.Model);
          
           if (_projects == null)
                    Projects = new ObservableCollection<Project>();

            Projects.Add(result.Model);
          
        }
    }
}
