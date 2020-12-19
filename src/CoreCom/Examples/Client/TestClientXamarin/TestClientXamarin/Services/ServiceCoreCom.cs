using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TestClientXamarin.Messages;
using TestClientXamarin.Repository;
using WallTec.CoreCom.Client;
using WallTec.CoreCom.Client.Models;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Sheard;
using Xamarin.Essentials;
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
        private async Task<string> Authenticate(CoreComOptions coreComOptions, string username,string password)
        {
            try
            {
  
            Console.WriteLine($"Authenticating as {username}...");
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{coreComOptions.ServerAddress}/generateJwtToken?username={HttpUtility.UrlEncode(username)}&password={HttpUtility.UrlEncode(password)}"),
                Method = HttpMethod.Get,
                Version = new Version(2, 0)
            };
            var tokenResponse = await httpClient.SendAsync(request);
            tokenResponse.EnsureSuccessStatusCode();

            var token = await tokenResponse.Content.ReadAsStringAsync();
            Console.WriteLine("Successfully authenticated.");

            return token;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        


        public async Task<bool> SetupCoreComServer()
        {

            //Setup events
            _coreComClient.Register(GetAllProjects, CoreComSignatures.ResponseAllProjects, new List<Project>().GetType());
            _coreComClient.Register(GetAddedProject, CoreComSignatures.AddProject, new Project().GetType());

            CoreComOptions coreComOptions = new CoreComOptions();
            //local debug
            //coreComOptions.ServerAddress =  (Device.RuntimePlatform == Device.Android ? "https://10.0.2.2:5001" : "https://192.168.2.121:5001");
            //azure debug
            coreComOptions.ServerAddress =  "https://wallteccorecomtestserver.azurewebsites.net";
           

            //Get Token
            var token = await Authenticate(coreComOptions, "demo","1234");
            if (string.IsNullOrEmpty(token))
                return false;

            coreComOptions.ClientToken = token;
            //Cross-Platform Identifier for the app stay the same as long the app is installed 
            var id = Preferences.Get("my_id", string.Empty);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = System.Guid.NewGuid().ToString();
                Preferences.Set("my_id", id);
            }
            coreComOptions.ClientId = id;

            _coreComClient.Connect(coreComOptions);
            //if (!response)
            //{
            //    return false;
            //}
           
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
        public async void SendAuthAsync(object outgoingObject, string messageSignature)
        {
            await _coreComClient.SendAuthAsync(outgoingObject, messageSignature);
        }
        public async void SendAuthAsync(string messageSignature)
        {
            await _coreComClient.SendAuthAsync(messageSignature);
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
