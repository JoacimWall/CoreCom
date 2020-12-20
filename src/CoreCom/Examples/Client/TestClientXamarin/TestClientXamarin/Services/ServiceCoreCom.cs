using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Grpc.Core;
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
        private CoreComClient _coreComClient = new CoreComClient();
        private CoreComOptions  _coreComOptions = new CoreComOptions();
        private static InMemoryData _inMemoryData = new InMemoryData();

        public CoreComClient CoreComClient
        {
            get { return _coreComClient; }

        }
        public ServiceCoreCom()
        {
            _coreComClient.OnConnectionStatusChange += _coreComClient_OnConnectionStatusChange;
            _coreComClient.OnLatestRpcExceptionChange += _coreComClient_OnLatestRpcExceptionChange;
        }

        private void _coreComClient_OnLatestRpcExceptionChange(object sender, RpcException e)
        {
            if (e != null)
            {
                LatestRpcException = e.Status.Detail;
            }
            else
            {
                LatestRpcException = string.Empty;
            }
        }

        private void _coreComClient_OnConnectionStatusChange(object sender, ConnectionStatus e)
        {
            ConnectionStatus = e;
        }
        private ConnectionStatus _connectionStatus;
        public ConnectionStatus ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }

            set
            {
                if (value != _connectionStatus)
                {
                   SetProperty(ref _connectionStatus , value);
                   // NotifyPropertyChanged();
                }
            }
        }
        private string _latestRpcException;
        public string LatestRpcException
        {
            get
            {
                return _latestRpcException;
            }

            set
            {
                if (value != _latestRpcException)
                {
                    SetProperty(ref _latestRpcException, value);
                }
            }
        }
        private async Task<bool> Authenticate(string username, string password)
        {
            try
            {

                Console.WriteLine($"Authenticating as {username}...");
                var httpClient = new HttpClient();
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{_coreComOptions.ServerAddress}/generateJwtToken?username={HttpUtility.UrlEncode(username)}&password={HttpUtility.UrlEncode(password)}"),
                    Method = HttpMethod.Get,
                    Version = new Version(2, 0)
                };
                var tokenResponse = await httpClient.SendAsync(request);
                tokenResponse.EnsureSuccessStatusCode();

                var token = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine("Successfully authenticated.");
                string[] values = token.Split("|");

                _coreComOptions.ClientToken = values[0];
                _coreComOptions.ClientId = values[1];

                return true;
            }
            catch (Exception ex)
            {
               await App.Current.MainPage.DisplayAlert("CoreCom", ex.Message + " Press Reauthorize try again", "Ok");
                return false;
            }
        }



        public async Task<bool> SetupCoreComServer()
        {

            //Setup events
            _coreComClient.Register(GetAllProjects, CoreComSignatures.ResponseAllProjects, new List<Project>().GetType());
            _coreComClient.Register(GetAddedProject, CoreComSignatures.AddProject, new Project().GetType());

            //local debug
            //coreComOptions.ServerAddress =  (Device.RuntimePlatform == Device.Android ? "https://10.0.2.2:5001" : "https://192.168.2.121:5001");
            //azure debug
            _coreComOptions.ServerAddress = "https://wallteccorecomtestserver.azurewebsites.net";
            _coreComOptions.ProcessQueueIntervalSec = 10;
            
            return true;
        }
        public async Task<bool> ConnectCoreComServer()
        {

            #region "Authentication with backen token and clientId from database"
            //coreComOptions.ClientId and coreComOptions.ClientToken is set inside the Authenticate method
            string username = (Device.RuntimePlatform == Device.Android ? "demoDroid" : "demoIos"); //simulate diffrent user
            var token = await Authenticate(username, "1234");
            if (!token)
                return false;
            #endregion

            #region "No Authentication"
            //Cross-Platform Identifier for the app stay the same as long the app is installed
            //in this senario all backend API is public and the server use guid below to seperate diffrent users requests
            //var id = Preferences.Get("my_id", string.Empty);
            //if (string.IsNullOrWhiteSpace(id))
            //{
            //    id = System.Guid.NewGuid().ToString();
            //    Preferences.Set("my_id", id);
            //}
            //coreComOptions.ClientId = id;
            #endregion

            _coreComClient.Connect(_coreComOptions);

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
