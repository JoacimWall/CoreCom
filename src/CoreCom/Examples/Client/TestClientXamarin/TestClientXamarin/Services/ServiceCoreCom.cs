using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TestClientXamarin.Messages;
using TestClientXamarin.Repository;
using WallTec.CoreCom.Client;
using WallTec.CoreCom.Client.Models;
using WallTec.CoreCom.Example.SharedObjects;
using WallTec.CoreCom.Sheard;
using Xamarin.Forms;

namespace TestClientXamarin.Services
{

    public class ServiceCoreCom : MvvmHelpers.ObservableObject
    {
        private  CoreComClient _coreComClient = new CoreComClient();
        private static InMemoryData _inMemoryData = new InMemoryData();
        
        public CoreComClient CoreComClient
        {
            get { return _coreComClient; }

        }
        public ServiceCoreCom()
        {
          


        }

       

        public async Task<bool> SetupCoreComServer()
        {

            CoreComOptions coreComOptions = new CoreComOptions();
            //local debug
            coreComOptions.ServerAddress =  (Device.RuntimePlatform == Device.Android ? "https://10.0.2.2:5001" : "https://192.168.2.121:5001");
            //azure debug
            //coreComOptions.ServerAddress =  "https://corecomtestappservice.azurewebsites.net";

            coreComOptions.ClientIsMobile = true;
            _coreComClient.Connect(coreComOptions);
            //if (!response)
            //{
            //    return false;
            //}
            //Setup events
            _coreComClient.Register(GetAllProjects, CoreComSignatures.ResponseAllProjects, new List<Project>().GetType());
            _coreComClient.Register(GetAddedProject, CoreComSignatures.AddProject, new Project().GetType());
            return true;
        }

        public async void SendAsync(object outgoingObject, string messageSignature)
        {
            await _coreComClient.SendAsync(outgoingObject, messageSignature);
        }
        public async void SendAsync(string messageSignature)
        {
            await _coreComClient.SendAsync(messageSignature);
        }
        private static async Task GetAllProjects(object value, CoreComUserInfo coreComUserInfo)
        {
            var project = value as List<Project>;
            _inMemoryData.Projects = project;
            MessagingCenter.Send<List<Project>>(project, MessageConstants.AllProjectsListUpdate);

        }
        private static async Task GetAddedProject(object value, CoreComUserInfo coreComUserInfo)
        {
            var project = value as Project;
            _inMemoryData.Projects.Add(project);
            MessagingCenter.Send<Project>(project, MessageConstants.AddedProjectsListUpdate);

        }
    }
}
