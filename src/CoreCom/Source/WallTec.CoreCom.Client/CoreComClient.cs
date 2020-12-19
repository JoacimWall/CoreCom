using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
using WallTec.CoreCom.Sheard;
using System.Linq;
using System.Timers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WallTec.CoreCom.Client.Models;
using Grpc.Net.Client;
using Grpc.Core;
using Grpc.Net.Client.Web;
using System.Net.Http;
using WallTec.CoreCom.Proto;

namespace WallTec.CoreCom.Client
{
    public enum ConnectionStatus 
    {
        Disconnected=0,
        Connecting = 1,
        Connected =2

    }
    public class CoreComClient : INotifyPropertyChanged
    {
        #region Private Propertys
        private GrpcWebHandler _httpHandler;
        private GrpcChannel _channel;
        private CoreComOptions _coreComOptions;
        private Proto.CoreCom.CoreComClient _coreComClient;
        private readonly List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private readonly List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>>();
        private readonly List<MessageCureRecord> _messagesOutgoing = new List<MessageCureRecord>();
        
        //Offline Propertys
        private System.Timers.Timer _timer;
        private System.Timers.Timer _checkCueTimer;

        #endregion
        #region Public Propertys
        public event PropertyChangedEventHandler PropertyChanged;
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
                    _connectionStatus = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private RpcException _latestRpcException;
        public RpcException LatestRpcException
        {
            get
            {
                return _latestRpcException;
            }

            set
            {
                if (value != _latestRpcException)
                {
                    _latestRpcException = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        #endregion
        #region Public Functions
        public CoreComClient()
        {
            // Create a timer with a ten second interval.
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnConnectTimedEvent;


            _checkCueTimer = new System.Timers.Timer(30000);
            _checkCueTimer.Elapsed += _checkCueTimer_Elapsed;

            // Register for connectivity changes, be sure to unsubscribe when finished
            //Xamarin.Essentials.Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        //void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        //{
        //    var access = e.NetworkAccess;
        //    var profiles = e.ConnectionProfiles;
        //}


        public bool Connect(CoreComOptions coreComOptions)
        {
            _coreComOptions = coreComOptions;

            if (coreComOptions.ProcessQueueIntervalSec >= 0)
                _checkCueTimer.Interval = coreComOptions.ProcessQueueIntervalSec * 1000;

            //start timmer for connect to server
            _timer.Enabled = true;
            

            return true;
        }
        //public async Task<bool> ShutdownAsync()
        //{
        //    await _channel.ShutdownAsync();
        //    return true;
        //}
        public void Register(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParm.Add(Tuple.Create(callback, messageSignature));


        }
        public void Register(Func<object, CoreComUserInfo, Task> callback, string messageSignature, Type type)
        {
            _receiveDelegatesTwoParm.Add(Tuple.Create(callback, messageSignature, type));


        }

        public async void CheckServerCue()
        {
           await SendAsync(CoreComInternalSignatures.CoreComInternal_PullCue);
        }
        public async Task<bool> SendAsync(object outgoingObject, string messageSignature)
        {
            return await SendInternalAsync(outgoingObject, messageSignature,false);
        }
        public async Task<bool> SendAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature,false);
        }
        public async Task<bool> SendAuthAsync(object outgoingObject, string messageSignature)
        {
            return await SendInternalAsync(outgoingObject, messageSignature,true);
        }
        public async Task<bool> SendAuthAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature,true);
        }
        #endregion

        #region Private Functions
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async void OnConnectTimedEvent(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            //First time 1 sec
            if (_timer.Interval == Convert.ToDouble(1000))
            {   //after connection error wait 10 sec to try
                _timer.Interval = 10000;
            }
            await OpenChannel();

        }
        private async void _checkCueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _checkCueTimer.Enabled = false;
            await SendAsync(CoreComInternalSignatures.CoreComInternal_PullCue);
        }

        private CallOptions GetCallOptions(bool isConnectToServer=false,bool addAuth = false )
        {

            int deadlineSec;
            if (isConnectToServer)
                deadlineSec = _coreComOptions.ConnectToServerDeadlineSec;
            else
                deadlineSec = _coreComOptions.MessageDeadlineSec;

            CallOptions calloptions;
            if (addAuth && !string.IsNullOrEmpty(_coreComOptions.ClientToken))
            {
                var headers = new Metadata();
                headers.Add("Authorization", $"Bearer {_coreComOptions.ClientToken}");
                calloptions = new CallOptions(headers);//.WithWaitForReady(true)
                calloptions = calloptions.WithDeadline(DateTime.UtcNow.AddSeconds(deadlineSec));
            }
            else
            {
                calloptions = new CallOptions();//.WithWaitForReady(true)
                calloptions = calloptions.WithDeadline(DateTime.UtcNow.AddSeconds(deadlineSec));
            }
            return calloptions;
        }
        private async Task<bool> OpenChannel()
        {
            try
            {
                if (_connectionStatus == ConnectionStatus.Connecting)
                    return false;

                ConnectionStatus = ConnectionStatus.Connecting;
                
                _httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText,new HttpClientHandler());
                //if (_coreComOptions.ClientIsMobile)
                //{  
                //    //case Device.Android:
                //    //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                //    _httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                //}
                //Check if the channel is open den shutdown before create new
                if (_channel != null)
                    await _channel.ShutdownAsync();

                _channel = GrpcChannel.ForAddress($"{_coreComOptions.ServerAddress}", new GrpcChannelOptions
                {
                    HttpHandler =  _httpHandler
                });

                if (_coreComClient == null)
                    _coreComClient = null;

                _coreComClient = new Proto.CoreCom.CoreComClient(_channel);
               
                //Wait for channel to open for 5 sec default
                var response = await _coreComClient.ClientConnectToServerAsync(new ConnectToServerRequest
                                    { ClientId = _coreComOptions.ClientId }, GetCallOptions(true).WithWaitForReady(true));

                Console.WriteLine("Connected to Server " + _coreComOptions.ServerAddress);

                ConnectionStatus = ConnectionStatus.Connected;
                //Start timmer for check cue server and client
                if (_coreComOptions.ProcessQueueIntervalSec > 0)
                    _checkCueTimer.Enabled = true;

                LatestRpcException = null;
                //send current cue
               // var result = await SendCue();

                return true;
            }
            catch (RpcException ex)
            {
                LatestRpcException = ex;
                if (ex.StatusCode == StatusCode.DeadlineExceeded ||
                    ex.StatusCode == StatusCode.PermissionDenied ||
                    ex.StatusCode == StatusCode.Unavailable)
                {

                    ConnectionStatus = ConnectionStatus.Disconnected;
                    _timer.Enabled = true;

                }

                return false;

            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionStatus.Disconnected;
                _timer.Enabled = true;
                return false;
            }
            finally
            {
                

            }

        }
        
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature,bool sendAuth)
        {
            string jsonObjectType = string.Empty;
            string jsonObject= string.Empty;
            CoreComMessage coreComMessage;
            
            try
            {
                //Turn of timmer for message cue as we get the cue from this call
                _checkCueTimer.Enabled = false;
                //error report to client
                if (outgoingObject != null)
                {
                    jsonObjectType = outgoingObject.GetType().ToString();
                    jsonObject = JsonSerializer.Serialize(outgoingObject);
                }
                
                coreComMessage = new CoreComMessage
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    ClientId = _coreComOptions.ClientId.ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };


           
                 _messagesOutgoing.Add(new MessageCureRecord { CoreComMessage = coreComMessage, SendAuth =sendAuth } );

                bool exit = false; 
                while (_messagesOutgoing.Count > 0 && ConnectionStatus == ConnectionStatus.Connected && !exit)
                {
                    AsyncServerStreamingCall<CoreComMessage> streamingCall;
                    if (_messagesOutgoing[0].SendAuth)
                    {
                        streamingCall = _coreComClient.SubscribeServerToClientAuth(_messagesOutgoing[0].CoreComMessage, GetCallOptions(false,sendAuth));
                    }
                    else
                    {
                        streamingCall = _coreComClient.SubscribeServerToClient(_messagesOutgoing[0].CoreComMessage, GetCallOptions(false, sendAuth));
                    }
                    //using var streamingCall = _coreComClient.SubscribeServerToClient(_messagesOutgoing[0].CoreComMessage, GetCallOptions());
                    
                    try
                    {
                        await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync())
                        {
                           await ParseServerToClientMessage(returnMessage);
                        }
                        _messagesOutgoing.RemoveAt(0);
                    }
                    catch (RpcException ex) 
                    {
                        ConnectionStatus = ConnectionStatus.Disconnected; 
                        LatestRpcException = ex;
                        exit = true;
                        switch (ex.StatusCode)
                        {
                            case StatusCode.Cancelled:
                                Console.WriteLine("Stream cancelled.");
                                break;
                            case StatusCode.PermissionDenied:
                            case StatusCode.Unavailable:
                                Console.WriteLine("PermissionDenied/Unavailable");
                                if (!_timer.Enabled)
                                    _timer.Enabled = true;
                                break;
                            case StatusCode.Unauthenticated:
                                Console.WriteLine("Unauthenticated.");
                                break; 
                            default:
                                break;
                        }
                        
                    }
                }
                //Start timmer for check cue server and client
                if (_coreComOptions.ProcessQueueIntervalSec > 0)
                    _checkCueTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                throw ex;
                
            }
           

            return true;
        }
     
        private async Task ParseServerToClientMessage(CoreComMessage request)
        {
            if (request.MessageSignature.StartsWith("CoreComInternal"))
            {
                await ParseCoreComFrameworkFromServerMessage(request);
                return;
            }


            CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId =  request.ClientId };
            if (string.IsNullOrEmpty(request.JsonObject))
            {
                var funcToRun = _receiveDelegatesOneParm.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                if (funcToRun != null)
                {
                    await funcToRun.Item1.Invoke(coreComUserInfo);
                }
                else
                {
                    //TODO:Report error
                }
            }
            else
            {
                var funcToRun = _receiveDelegatesTwoParm.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                if (funcToRun != null)
                {
                    var objectDeser = JsonSerializer.Deserialize(request.JsonObject, funcToRun.Item3);
                    await funcToRun.Item1.Invoke(objectDeser, coreComUserInfo);
                }
                else
                {
                    //TODO:Report error
                }
            }
        }
        private async Task ParseCoreComFrameworkFromServerMessage(CoreComMessage request)
        {

        }
        

        #endregion
    }
}
