using System;
using System.Threading.Tasks;
using System.Text.Json;
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
using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Client
{
    
    public class CoreComClient
    {
        #region Private Propertys
        private GrpcWebHandler _httpHandler;
        private GrpcChannel _channel;
        private CoreComOptions _coreComOptions;
        private Proto.CoreCom.CoreComClient _coreComClient;
        private readonly List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private readonly List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>>();

        //Offline Propertys
        private System.Timers.Timer _timer;
        private System.Timers.Timer _checkCueTimer;
        private ConnectionStatusEnum _connectionStatus;
        private RpcException _latestRpcException;
        private DbContextOptions _dbContextOptions;
        //Events
        public event EventHandler<ConnectionStatusEnum> OnConnectionStatusChange;
        protected virtual void ConnectionStatusChange(ConnectionStatusEnum e)
        {
            _connectionStatus = e;
            EventHandler<ConnectionStatusEnum> handler = OnConnectionStatusChange;
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
        public event EventHandler<LogEvent> OnLogEventOccurred;
        protected virtual void LogEventOccurred(LogEvent e)
        {

            //_latestRpcException = e;
            EventHandler<LogEvent> handler = OnLogEventOccurred;
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
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnConnectTimedEvent;

            _checkCueTimer = new System.Timers.Timer(30000);
            _checkCueTimer.Elapsed += _checkCueTimer_Elapsed;

            _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                    .UseInMemoryDatabase(databaseName: "CoreComDb").Options;

        }

        public async Task<bool> Disconnect()
        {
            try
            {

                //start timmer for connect to server
                _timer.Enabled = false;
                if (_connectionStatus == ConnectionStatusEnum.Connected)
                {
                    var response = _coreComClient.ClientDisconnectFromServer(new DisconnectFromServerRequest
                    { ClientId = _coreComOptions.ClientId }, GetCallOptions(false));
                    //TODO: log disconnected
                }

                _coreComClient = null;
                await _channel.ShutdownAsync();

            }
            catch (Exception ex)
            {

            }
            finally
            {
                ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
            }

            return true;
        }
        public bool Connect(CoreComOptions coreComOptions)
        {
            _coreComOptions = coreComOptions;

            //start timmer for connect to server
            _timer.Interval = Convert.ToDouble(1000);
            _timer.Enabled = true;

            _checkCueTimer.Interval = Convert.ToDouble(coreComOptions.RequestServerQueueIntervalSec * 1000);

            return true;
        }

        public void Register(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParm.Add(Tuple.Create(callback, messageSignature));
        }
        public void Register(Func<object, CoreComUserInfo, Task> callback, string messageSignature, Type type)
        {
            //var parameter= callback.Method.GetParameters().First();

            _receiveDelegatesTwoParm.Add(Tuple.Create(callback, messageSignature, type));
        }

        public async void CheckServerCue()
        {
            await SendAsync(CoreComInternalSignatures.CoreComInternal_PullQueue).ConfigureAwait(false);
        }
        public async Task<bool> SendAsync(object outgoingObject, string messageSignature)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, false).ConfigureAwait(false);
        }
        public async Task<bool> SendAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature, false).ConfigureAwait(false);
        }
        public async Task<bool> SendAuthAsync(object outgoingObject, string messageSignature)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, true).ConfigureAwait(false);
        }
        public async Task<bool> SendAuthAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature, true).ConfigureAwait(false);
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
            await OpenChannel().ConfigureAwait(false);

        }
        private async void _checkCueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _checkCueTimer.Enabled = false;
            await SendAsync(CoreComInternalSignatures.CoreComInternal_PullQueue).ConfigureAwait(false);
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
                if (_connectionStatus == ConnectionStatusEnum.Connecting)
                    return false;

                ConnectionStatusChange(ConnectionStatusEnum.Connecting);
                //this is so you can debug on mac and emulator the server has "EndpointDefaults": { "Protocols": "Http1"
                // Return `true` to allow certificates that are untrusted/invalid
                if (_coreComOptions.DangerousAcceptAnyServerCertificateValidator)
                    _httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
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

                if (_coreComClient != null)
                    _coreComClient = null;

                _coreComClient = new Proto.CoreCom.CoreComClient(_channel);

                //Wait for channel to open for 5 sec default
                var response = await _coreComClient.ClientConnectToServerAsync(new ConnectToServerRequest
                { ClientId = _coreComOptions.ClientId }, GetCallOptions(true).WithWaitForReady(true));

                Console.WriteLine("Connected to Server " + _coreComOptions.ServerAddress);

                ConnectionStatusChange(ConnectionStatusEnum.Connected);
                //Start timmer for check cue server and client
                if (_coreComOptions.RequestServerQueueIntervalSec > 0)
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

                    ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                    _timer.Enabled = true;
                }

                return false;

            }
            catch (Exception ex)
            {
                ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                _timer.Enabled = true;
                return false;
            }
        }
        private bool _workingOnCue = false;
        private async Task<bool> ProcessCue()
        {
            if (_workingOnCue || _connectionStatus != ConnectionStatusEnum.Connected)
                return true;

            try
            {
                _workingOnCue = true;
                using (var DbContext = new CoreComContext(_dbContextOptions))
                {
                    var outgoingMess = DbContext.OutgoingMessages.
                        Where(x => x.TransferStatus < (int)TransferStatusEnum.Transferred).ToList();

                    foreach (var item in outgoingMess)
                    {
                        if (item.SendAuth)
                        {
                            item.ClientId = _coreComOptions.ClientId.ToString();

                            using var streamingCall = _coreComClient.SubscribeServerToClientAuth(item, GetCallOptions(false, item.SendAuth));
                           
                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                if (item.TransferStatus == (int)TransferStatusEnum.New)
                                {
                                    item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                                    LogEventOccurred(new LogEvent { Title = item.MessageSignature, TransferStatus = (TransferStatusEnum)item.TransferStatus, MessageSize = item.CalculateSize() });
                                }
                                await ParseServerToClientMessage(returnMessage);
                            }
                            streamingCall.Dispose();
                        }
                        else
                        {
                            item.ClientId = _coreComOptions.ClientId.ToString();
                            using var streamingCall = _coreComClient.SubscribeServerToClient(item, GetCallOptions(false, item.SendAuth));

                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                if (item.TransferStatus == (int)TransferStatusEnum.New)
                                {
                                    item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                                    LogEventOccurred(new LogEvent { Title = item.MessageSignature, TransferStatus = (TransferStatusEnum)item.TransferStatus, MessageSize = item.CalculateSize() });
                                }
                                await ParseServerToClientMessage(returnMessage);
                            }
                            streamingCall.Dispose();
                        }
                        //Remove messages
                        item.TransferStatus = (int)TransferStatusEnum.Transferred;
                        await DbContext.SaveChangesAsync().ConfigureAwait(false);
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
                        ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
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
            if (_coreComOptions.RequestServerQueueIntervalSec > 0)
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
                    TransactionIdentifier = Guid.NewGuid().ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject,
                    SendAuth = sendAuth
                };

                using (var dbContext = new CoreComContext(_dbContextOptions))
                {

                    dbContext.OutgoingMessages.Add(coreComMessage);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }


                await ProcessCue().ConfigureAwait(false);
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
                await ParseCoreComFrameworkFromServerMessage(request).ConfigureAwait(false);
                return;
            }
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                request.TransferStatus = (int)TransferStatusEnum.Recived;
                dbContext.IncomingMessages.Add(request);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                LogEventOccurred(new LogEvent { Title = request.MessageSignature, TransferStatus = (TransferStatusEnum)request.TransferStatus, MessageSize = request.CalculateSize() });

                CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId = request.ClientId };
                if (string.IsNullOrEmpty(request.JsonObject))
                {
                    var funcToRun = _receiveDelegatesOneParm.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                    if (funcToRun != null)
                    {
                        await funcToRun.Item1.Invoke(coreComUserInfo).ConfigureAwait(false);
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
                        await funcToRun.Item1.Invoke(objectDeser, coreComUserInfo).ConfigureAwait(false);
                    }
                    else
                    {
                        //TODO:Report error
                    }
                }
            }
        }
        private async Task ParseCoreComFrameworkFromServerMessage(CoreComMessageResponse request)
        {

        }


        #endregion
    }
}
