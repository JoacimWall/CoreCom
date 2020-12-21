using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
using WallTec.CoreCom.Sheard;
using System.Linq;
using System.Timers;
using WallTec.CoreCom.Client.Models;
using Grpc.Net.Client;
using Grpc.Core;
using Grpc.Net.Client.Web;
using System.Net.Http;
using WallTec.CoreCom.Proto;

namespace WallTec.CoreCom.Client
{
    public enum ConnectionStatus : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2

    }
    public class CoreComClient
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
        private ConnectionStatus _connectionStatus;
        private RpcException _latestRpcException;
        //Events
        public event EventHandler<ConnectionStatus> OnConnectionStatusChange;
        protected virtual void ConnectionStatusChange(ConnectionStatus e)
        {
            _connectionStatus = e;
            EventHandler<ConnectionStatus> handler = OnConnectionStatusChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<RpcException> OnLatestRpcExceptionChange;
        protected virtual void LatestRpcExceptionChange(RpcException e)
        {

            _latestRpcException = e;
            EventHandler<RpcException> handler = OnLatestRpcExceptionChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion
        #region Public Propertys

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
            return await SendInternalAsync(outgoingObject, messageSignature, false);
        }
        public async Task<bool> SendAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature, false);
        }
        public async Task<bool> SendAuthAsync(object outgoingObject, string messageSignature)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, true);
        }
        public async Task<bool> SendAuthAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature, true);
        }
        #endregion

        #region Private Functions

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

        private CallOptions GetCallOptions(bool isConnectToServer = false, bool addAuth = false)
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
        /// <summary>
        /// Open the channel to the server. this is trigger first time on connect or if the server has restared.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> OpenChannel()
        {
            try
            {
                if (_connectionStatus == ConnectionStatus.Connecting)
                    return false;

                ConnectionStatusChange(ConnectionStatus.Connecting);
                //this is so you can debug on mac and emulator the server has "EndpointDefaults": { "Protocols": "Http1"
                // Return `true` to allow certificates that are untrusted/invalid
                if (_coreComOptions.DangerousAcceptAnyServerCertificateValidator)
                     _httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText,  new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
                else
                {
                    _httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());
                }
               
                //Check if the channel is open den shutdown before create new
                if (_channel != null)
                    await _channel.ShutdownAsync();

                _channel = GrpcChannel.ForAddress($"{_coreComOptions.ServerAddress}", new GrpcChannelOptions
                {
                    HttpHandler = _httpHandler,
                    MaxReceiveMessageSize = null, // 5 * 1024 * 1024, // 5 MB
                    MaxSendMessageSize = null //2 * 1024 * 1024 // 2 MB
                });

                if (_coreComClient == null)
                    _coreComClient = null;

                _coreComClient = new Proto.CoreCom.CoreComClient(_channel);

                //Wait for channel to open for 5 sec default
                var response = await _coreComClient.ClientConnectToServerAsync(new ConnectToServerRequest
                { ClientId = _coreComOptions.ClientId }, GetCallOptions(true).WithWaitForReady(true));

                Console.WriteLine("Connected to Server " + _coreComOptions.ServerAddress);

                ConnectionStatusChange(ConnectionStatus.Connected);
                //Start timmer for check cue server and client
                if (_coreComOptions.ProcessQueueIntervalSec > 0)
                    _checkCueTimer.Enabled = true;

                LatestRpcExceptionChange(null);
                //send current cue
                // var result = await SendCue();

                return true;
            }
            catch (RpcException ex)
            {
                LatestRpcExceptionChange(ex);
                if (ex.StatusCode == StatusCode.DeadlineExceeded ||
                    ex.StatusCode == StatusCode.PermissionDenied ||
                    ex.StatusCode == StatusCode.Unavailable)
                {

                    ConnectionStatusChange(ConnectionStatus.Disconnected);
                    _timer.Enabled = true;
                }

                return false;

            }
            catch (Exception ex)
            {
                ConnectionStatusChange(ConnectionStatus.Disconnected);
                _timer.Enabled = true;
                return false;
            }
        }
        private bool _workingOnCue = false;
        private async Task<bool> ProcessCue()
        {
            if (_workingOnCue)
                return true;
            try
            {
                //get all messages that are in process or new
                //var cue = _messagesOutgoing.Where(x => x.CoreComMessage.TransferStatus == (int)TransferStatus.New || x.CoreComMessage.TransferStatus == (int)TransferStatus.InProcess)
                //    .OrderByDescending(s => s.CoreComMessage.TransferStatus).ToList();

                while (_messagesOutgoing.Any(x => x.CoreComMessage.TransferStatus < (int)TransferStatusEnum.Recived) && _connectionStatus == ConnectionStatus.Connected)
                {
                    _workingOnCue = true;
                    var message = _messagesOutgoing.FirstOrDefault(x => x.CoreComMessage.TransferStatus < (int)TransferStatusEnum.Recived);
                    message.CoreComMessage.TransferStatus = (int)TransferStatusEnum.InProcess;

                   // AsyncServerStreamingCall<CoreComMessageResponse> streamingCall;
                    if (message.SendAuth)
                    {
                        using var streamingCall = _coreComClient.SubscribeServerToClientAuth(message.CoreComMessage, GetCallOptions(false, message.SendAuth));

                        message.CoreComMessage.TransferStatus = (int)TransferStatusEnum.InProcess;

                        await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync())
                           if (returnMessage.MessageSignature == CoreComInternalSignatures.CoreComInternal_StatusUpdate)
                                //Update status
                                _messagesOutgoing.FirstOrDefault(x => x.CoreComMessage.TransactionIdentifier == returnMessage.TransactionIdentifier).CoreComMessage.TransferStatus = returnMessage.TransferStatus;
                            else  //parse message
                                await ParseServerToClientMessage(returnMessage);

                        streamingCall.Dispose();
                    }
                    else
                    {
                        using var streamingCall = _coreComClient.SubscribeServerToClient(message.CoreComMessage, GetCallOptions(false, message.SendAuth));

                        message.CoreComMessage.TransferStatus = (int)TransferStatusEnum.InProcess;

                        await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync())
                            if (returnMessage.MessageSignature == CoreComInternalSignatures.CoreComInternal_StatusUpdate)
                                //Update status
                                _messagesOutgoing.FirstOrDefault(x => x.CoreComMessage.TransactionIdentifier == returnMessage.TransactionIdentifier).CoreComMessage.TransferStatus = returnMessage.TransferStatus;
                            else  //parse message
                                await ParseServerToClientMessage(returnMessage);

                        streamingCall.Dispose();
                    }
                }
            }
            catch (RpcException ex)
            {
                _workingOnCue = false;
               
                LatestRpcExceptionChange(ex);
                switch (ex.StatusCode)
                {
                    case StatusCode.DeadlineExceeded:
                        break;
                    case StatusCode.Cancelled:
                        Console.WriteLine("Stream cancelled.");
                        break;
                    case StatusCode.PermissionDenied:
                    case StatusCode.Unavailable:
                        ConnectionStatusChange(ConnectionStatus.Disconnected);
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
            catch (Exception ex)
            {
                _workingOnCue = false;

            }
            _workingOnCue = false;
            //Start timmer for check cue server and client
            if (_coreComOptions.ProcessQueueIntervalSec > 0)
                _checkCueTimer.Enabled = true;

            return true;

        }
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature, bool sendAuth)
        {
            string jsonObjectType = string.Empty;
            string jsonObject = string.Empty;
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
                    CoreComMessageId = Guid.NewGuid().ToString(),
                    ClientId = _coreComOptions.ClientId.ToString(),
                    TransactionIdentifier =  Guid.NewGuid().ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };



                _messagesOutgoing.Add(new MessageCureRecord { CoreComMessage = coreComMessage, SendAuth = sendAuth });

                await ProcessCue();
            }
            catch (Exception ex)
            {
                throw ex;

            }


            return true;
        }

        private async Task ParseServerToClientMessage(CoreComMessageResponse request)
        {
            if (request.MessageSignature.StartsWith("CoreComInternal"))
            {
                await ParseCoreComFrameworkFromServerMessage(request);
                return;
            }


            CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId = request.ClientId };
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
        private async Task ParseCoreComFrameworkFromServerMessage(CoreComMessageResponse request)
        {

        }


        #endregion
    }
}
