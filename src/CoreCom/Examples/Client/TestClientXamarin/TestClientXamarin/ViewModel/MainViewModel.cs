using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TestClientXamarin.Messages;
using TestClientXamarin.Services;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Example.Shared.Entitys;
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

       

        public ICommand GetProjectsCommand => new Command(async () => await GetProjectsAsync());
        public ICommand AddProjectsCommand => new Command(async () => await AddProjectsAsync());
        
        private async Task AddProjectsAsync()
        {
            App.ServiceCoreCom.SendAuthAsync(new Project { Name = AddProjectName, Description = "project added from client" }, CoreComSignatures.AddProject);
            
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
