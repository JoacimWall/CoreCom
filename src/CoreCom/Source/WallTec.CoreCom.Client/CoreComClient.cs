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
        private Timer _timer;
        private Timer _checkCueTimer;
        private ConnectionStatusEnum _connectionStatus;
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

            EventHandler<RpcException> handler = OnLatestRpcExceptionChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<LogEvent> OnLogEventOccurred;
        internal async virtual void LogEventOccurred(CoreComContext dbContext, CoreComMessage coreComMessage)
        {

            LogEvent logEvent = new LogEvent { Description = coreComMessage.MessageSignature, TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus, MessageSize = coreComMessage.CalculateSize() };


            //Messages
            switch (_coreComOptions.LogMessageSource)
            {
                case LogMessageSourceEnum.Database:
                    //allways remove CoreComInternal from outgoingmessage table
                    if (coreComMessage.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue)
                        dbContext.OutgoingMessages.Remove(coreComMessage);

                    //it's allready in db just update status

                    break;
                case LogMessageSourceEnum.TextFile:
                    //TODO:Create textfile log
                    break;
                case LogMessageSourceEnum.NoLoging:
                    if (coreComMessage.TransferStatus != (int)TransferStatusEnum.New)
                        dbContext.OutgoingMessages.Remove(coreComMessage);
                    break;
                default:
                    break;
            }
            
            //Events
            switch (_coreComOptions.LogEventSource)
            {
                case LogEventSourceEnum.Database:
                    await dbContext.LogEvents.AddAsync(logEvent);

                    break;
                case LogEventSourceEnum.TextFile:
                    //TODO:Create textfile log

                    break;
                case LogEventSourceEnum.NoLoging:

                    break;
                default:
                    break;
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);


            EventHandler<LogEvent> handler = OnLogEventOccurred;
            if (handler != null)
            {
                handler(this, logEvent);
            }
        }

        internal async virtual void LogEventOccurred(CoreComContext dbContext, CoreComMessageResponse coreComMessageResponse)
        {

            LogEvent logEvent = new LogEvent { Description = coreComMessageResponse.MessageSignature, TransferStatus = (TransferStatusEnum)coreComMessageResponse.TransferStatus, MessageSize = coreComMessageResponse.CalculateSize() };


            //Messages
            switch (_coreComOptions.LogMessageSource)
            {
                case LogMessageSourceEnum.Database:
                    //allways remove CoreComInternal from incomming table/ the massage is new so it does not exist in table
                    if (!coreComMessageResponse.MessageSignature.StartsWith("CoreComInternal_"))
                    { //add incomming message to db
                        dbContext.IncomingMessages.Add(coreComMessageResponse);
                    }
                    break;
                case LogMessageSourceEnum.TextFile:
                    //TODO:Create textfile log
                    if (!coreComMessageResponse.MessageSignature.StartsWith("CoreComInternal_"))
                    { //add incomming message to file
                        
                    }
                    break;
                case LogMessageSourceEnum.NoLoging:
                    //dbContext.OutgoingMessages.Remove(coreComMessage);
                    break;
                default:
                    break;
            }

            //Events
            switch (_coreComOptions.LogEventSource)
            {
                case LogEventSourceEnum.Database:
                    if (coreComMessageResponse.TransferStatus != (int)TransferStatusEnum.New)
                        await dbContext.LogEvents.AddAsync(logEvent);

                    break;
                case LogEventSourceEnum.TextFile:
                    //TODO:Create textfile log

                    break;
                case LogEventSourceEnum.NoLoging:

                    break;
                default:
                    break;
            }
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            EventHandler<LogEvent> handler = OnLogEventOccurred;
            if (handler != null)
            {
                handler(this, logEvent);
            }
        }
        internal async virtual void LogEventOccurred(LogEvent logEvent)
        {
            using (var dbContext = new CoreComContext(_dbContextOptions))
            { 
                //Events
                switch (_coreComOptions.LogEventSource)
                {
                    case LogEventSourceEnum.Database:
                        await dbContext.LogEvents.AddAsync(logEvent);

                        break;
                    case LogEventSourceEnum.TextFile:
                        //TODO:Create textfile log

                        break;
                    case LogEventSourceEnum.NoLoging:

                        break;
                    default:
                        break;
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            EventHandler<LogEvent> handler = OnLogEventOccurred;
            if (handler != null)
            {
                handler(this, logEvent);
            }
        }
        #endregion
        #region Public Propertys

        #endregion
        #region Public Functions
        public CoreComClient()
        {
            _timer = new Timer(1000);
            _timer.Elapsed += OnConnectTimedEvent;

            _checkCueTimer = new Timer(30000);
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
                //Log outgoing meesage
                var connectMessage = new ConnectToServerRequest { ClientId = _coreComOptions.ClientId };

                var connectEvent = new LogEvent { Description = "Connected to Server ", TransferStatus = TransferStatusEnum.Transferred, MessageSize = connectMessage.CalculateSize() };
                LogEventOccurred(connectEvent);
                var response = await _coreComClient.ClientConnectToServerAsync(connectMessage, GetCallOptions(true).WithWaitForReady(true));
                //log request
               
                //log response
                LogEventOccurred(new LogEvent {Description = response.Response, TransferStatus = TransferStatusEnum.Recived, MessageSize = response.CalculateSize() });
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

                if (ex.StatusCode == StatusCode.PermissionDenied || ex.StatusCode == StatusCode.Unavailable)
                {
                    ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                    LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                    _timer.Enabled = true;
                }
                else if (ex.StatusCode == StatusCode.DeadlineExceeded)
                {
                    LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus =_connectionStatus });
                }
                return false;

            }
            catch (Exception ex)
            {
                ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
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
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    var outgoingMess = dbContext.OutgoingMessages.
                        Where(x => x.TransferStatus < (int)TransferStatusEnum.Transferred).ToList();

                    foreach (var item in outgoingMess)
                    {
                        if (item.SendAuth)
                        {
                            item.ClientId = _coreComOptions.ClientId.ToString();
                            LogEventOccurred(dbContext, item);
                            using var streamingCall = _coreComClient.SubscribeServerToClientAuth(item, GetCallOptions(false, item.SendAuth));
                           
                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                if (item.TransferStatus == (int)TransferStatusEnum.New)
                                {
                                    item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                    LogEventOccurred(dbContext, item);
                                }
                                await ParseServerToClientMessage(returnMessage);
                            }
                            streamingCall.Dispose();
                        }
                        else
                        {
                            item.ClientId = _coreComOptions.ClientId.ToString();
                            LogEventOccurred(dbContext, item);
                            using var streamingCall = _coreComClient.SubscribeServerToClient(item, GetCallOptions(false, item.SendAuth));

                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                if (item.TransferStatus == (int)TransferStatusEnum.New)
                                {
                                    item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                    LogEventOccurred(dbContext, item);
                                }
                                await ParseServerToClientMessage(returnMessage);
                            }
                            streamingCall.Dispose();
                        }

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
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        break;
                    case StatusCode.Cancelled:
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        Console.WriteLine("Stream cancelled.");
                        break;
                    case StatusCode.PermissionDenied:
                    case StatusCode.Unavailable:
                        ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        Console.WriteLine("PermissionDenied/Unavailable");
                        if (!_timer.Enabled)
                            _timer.Enabled = true;
                        break;
                    case StatusCode.Unauthenticated:
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        Console.WriteLine("Unauthenticated.");
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
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
            
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                request.TransferStatus = (int)TransferStatusEnum.Recived;

                if (request.MessageSignature.StartsWith("CoreComInternal_"))
                {
                    await ParseCoreComFrameworkFromServerMessage(request).ConfigureAwait(false);
                    LogEventOccurred(dbContext, request);
                    return;
                }



                LogEventOccurred(dbContext, request);

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
