using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TestClientXamarin.Messages;
using TestClientXamarin.Services;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Example.Shared.Entitys;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TestClientXamarin.ViewModel
{
    public class MainViewModel : MvvmHelpers.BaseViewModel
    {
        public MainViewModel()
        {
           
            MessagingCenter.Subscribe<List<Project>>(this, MessageConstants.AllProjectsListUpdate, (sender) =>
            {
                Projects = new ObservableCollection<Project>(sender);
            });
            MessagingCenter.Subscribe<Project>(this, MessageConstants.AddedProjectsListUpdate, (sender) =>
            {
                if (_projects == null)
                    Projects = new ObservableCollection<Project>();
                Projects.Add(sender);
            });

        }

        public ICommand CheckCueCommand => new Command(async () => await CheckCueCommandAsync());
        public ICommand ConnectToServerCommand => new Command(async () => await ConnectCommandAsync());

        private async Task ConnectCommandAsync()
        {
            await App.ServiceCoreCom.ConnectCoreComServer();
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
        private async Task CheckCueCommandAsync()
        {
            App.ServiceCoreCom.CoreComClient.CheckServerCue();
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
    }
}
