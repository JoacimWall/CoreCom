using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Grpc.Core;
using WallTec.CoreCom.Client;
using WallTec.CoreCom.Client.Models;
using WallTec.CoreCom.Sheard;
using WallTec.CoreCom.Sheard.Models;
using Xamarin.Forms;

namespace TestClientXamarin.Services
{

    public class ServiceCoreCom : MvvmHelpers.ObservableObject
    {
        public CoreComClient CoreComClient = new CoreComClient();
        private CoreComOptions _coreComOptions;
        
        public ServiceCoreCom()
        {
            CoreComClient.OnConnectionStatusChange += _coreComClient_OnConnectionStatusChange;
            CoreComClient.OnLatestRpcExceptionChange += _coreComClient_OnLatestRpcExceptionChange;
            CoreComClient.OnLogEventOccurred += _coreComClient_OnLogEventOccurred;
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
        private void _coreComClient_OnConnectionStatusChange(object sender, ConnectionStatusEnum e)
        {
            ConnectionStatus = e;
        }
        private void _coreComClient_OnLogEventOccurred(object sender, LogEvent e)
        {

            if (e != null)
            {
                LatestLogEvent = e;
            }


        }
        private ConnectionStatusEnum _connectionStatus;
        public ConnectionStatusEnum ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }

            set
            {
                if (value != _connectionStatus)
                {
                    SetProperty(ref _connectionStatus, value);
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
        private LogEvent _latestLogEvent;
        public LogEvent LatestLogEvent
        {
            get
            {
                return _latestLogEvent;
            }

            set
            {
                if (value != _latestLogEvent)
                {
                    SetProperty(ref _latestLogEvent, value);
                }
            }
        }
        
        public async Task<bool> Authenticate(string username, string password)
        {
            try
            {
               App.ConsoleWriteLineDebug($"Authenticating as {username}...");
                var httpClientHandler = new HttpClientHandler();
                //this is so you can debug on mac and emulator the server has "EndpointDefaults": { "Protocols": "Http1"
                // Return `true` to allow certificates that are untrusted/invalid
                if (_coreComOptions.DangerousAcceptAnyServerCertificateValidator)
                    httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                var httpClient = new HttpClient(httpClientHandler);

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{_coreComOptions.ServerAddress}/generateJwtToken?username={HttpUtility.UrlEncode(username)}&password={HttpUtility.UrlEncode(password)}"),
                    Method = HttpMethod.Get,
                    Version = new Version(2, 0),

                };
                var tokenResponse = await httpClient.SendAsync(request);
                tokenResponse.EnsureSuccessStatusCode();

                var token = await tokenResponse.Content.ReadAsStringAsync();
                App.ConsoleWriteLineDebug("Successfully authenticated.");
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

        public bool SetupCoreComServer()
        {

            

            //local debug
            _coreComOptions = new CoreComOptions
            {   //debug on android emulator
                ServerAddress = (Device.RuntimePlatform == Device.Android ? "https://10.0.2.2:5001" : "https://localhost:5001"),
                //azure debug
                //ServerAddress = "https://wallteccorecomtestserver.azurewebsites.net",
                DatabaseMode = DatabaseModeEnum.UseImMemory,
                GrpcOptions = new GrpcOptions
                {
                    RequestServerQueueIntervalSec = 30,
                    MessageDeadlineSec = 30
                },
                LogSettings = new LogSettings
                    {
                        LogErrorTarget = LogErrorTargetEnum.NoLoging,
                        LogEventTarget = LogEventTargetEnum.NoLoging,
                        LogMessageTarget = LogMessageTargetEnum.NoLoging

                }

            };
            
            //Debug local on mac where the server is running in "Kestrel": { "EndpointDefaults": { "Protocols": "Http1"  }  }
#if DEBUG
            _coreComOptions.DangerousAcceptAnyServerCertificateValidator = true;
#endif
            return true;
        }
       
        public async Task<bool> ConnectCoreComServer()
        {

            #region "Authentication with backen token and clientId from database"
            //coreComOptions.ClientId and coreComOptions.ClientToken is set inside the Authenticate method
            string username = (Device.RuntimePlatform == Device.Android ? "demoDroid" : "demoIos"); //simulate diffrent user
            var token = await Authenticate(username, "1234").ConfigureAwait(false);
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
           
            CoreComClient.Connect(_coreComOptions);
            
            return true;
        }

        public bool DisconnectCoreComServer()
        {
            CoreComClient.Disconnect();

          
            return true;
        }
        public void CheckServerQueue()
        {
            CoreComClient.CheckServerQueue();
           
        }
        public async void SendAsync(object outgoingObject, string messageSignature)
        {
            await CoreComClient.SendAsync(outgoingObject, messageSignature);
        }
        public async void SendAsync(string messageSignature)
        {
            await CoreComClient.SendAsync(messageSignature);
        }
        public async void SendAuthAsync(object outgoingObject, string messageSignature)
        {
            await CoreComClient.SendAuthAsync(outgoingObject, messageSignature);
        }
        public async void SendAuthAsync(string messageSignature)
        {
            await CoreComClient.SendAuthAsync(messageSignature);
        }
       
    }
}
