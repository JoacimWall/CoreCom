using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Timers;
using WallTec.CoreCom.Client.Models;
using Grpc.Net.Client;
using Grpc.Core;
using Grpc.Net.Client.Web;
using System.Net.Http;
using WallTec.CoreCom.Proto;
using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Models;
using System.IO;
using Xamarin.Essentials;

namespace WallTec.CoreCom.Client
{

    public class CoreComClient
    {
        #region Private Propertys/Events
        private GrpcWebHandler _httpHandler;
        private GrpcChannel _channel;
        private CoreComOptions _coreComOptions;
        private Proto.CoreCom.CoreComClient _coreComClient;
        private Timer _timer;
        private Timer _checkQueueTimer;
        private ConnectionStatusEnum _connectionStatus;
        private DbContextOptions _dbContextOptions;
        private bool _workingOnQueue = false;
        //Events
        public event EventHandler<ConnectionStatusEnum> OnConnectionStatusChange;
        public event EventHandler<LogEvent> OnLogEventOccurred;
        public event EventHandler<RpcException> OnLatestRpcExceptionChange;
        public event EventHandler<LogError> OnLogErrorOccurred;


        #endregion
        #region Public Propertys

        #endregion
        #region Public Functions
        public CoreComClient()
        {
            _timer = new Timer(1000);
            _timer.Elapsed += OnConnectTimedEvent;

            _checkQueueTimer = new Timer(30000);
            _checkQueueTimer.Elapsed += _checkQueueTimer_Elapsed;
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
                    //log disconnected
                    LogEventOccurred(new LogEvent { ClientId = _coreComOptions.ClientId, Description = response.Response });
                }

                _coreComClient = null;
                await _channel.ShutdownAsync();

            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessageResponse { ClientId = _coreComOptions.ClientId });
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

            _checkQueueTimer.Interval = Convert.ToDouble(coreComOptions.GrpcOptions.RequestServerQueueIntervalSec * 1000);
            switch (coreComOptions.DatabaseMode)
            {
                case DatabaseModeEnum.UseImMemory:
                    _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                    .UseInMemoryDatabase(databaseName: "CoreComDb").Options;
                    break;
                case DatabaseModeEnum.UseSqlite:
                    string dbPath = Path.Combine(FileSystem.AppDataDirectory, "CoreComDb.db3");
                    _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                    .UseSqlite($"Filename={dbPath}").Options;

                    break;

                default:
                    break;
            }
            return true;
        }
        public void Register(string message, Action callback) 
        {
            CoreComMessagingCenter.Subscribe(message, callback,false);
        }
        public void Register<Targs>(string message, Action<Targs> callback) where Targs : class
        {
            CoreComMessagingCenter.Subscribe<Targs>(message, callback,false);
        }
        public void UnRegister(string message)
        {
            CoreComMessagingCenter.Unsubscribe(message);
        }
        public async void CheckServerQueue()
        {
            await SendAsync(CoreComInternalSignatures.CoreComInternal_PullQueue,false).ConfigureAwait(false);
        }
        public async Task<bool> SendAsync(object outgoingObject, string messageSignature,bool noDeadline=false)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, false,noDeadline).ConfigureAwait(false);
        }
        public async Task<bool> SendAsync(string messageSignature,bool noDeadline)
        {
            return await SendInternalAsync(null, messageSignature, false,  noDeadline).ConfigureAwait(false);
        }
        public async Task<bool> SendAuthAsync(object outgoingObject, string messageSignature, bool noDeadline)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, true,  noDeadline).ConfigureAwait(false);
        }
        public async Task<bool> SendAuthAsync(string messageSignature, bool noDeadline)
        {
            return await SendInternalAsync(null, messageSignature, true,  noDeadline).ConfigureAwait(false);
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
        private async void _checkQueueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _checkQueueTimer.Enabled = false;
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                var outgoingMess = dbContext.OutgoingMessages.
                    Where(x => x.TransferStatus < (int)TransferStatusEnum.Transferred).ToList();

                if (outgoingMess.Count == 0)
                    await SendInternalAsync(null, CoreComInternalSignatures.CoreComInternal_PullQueue, false,false);
                else
                    await ProcessQueue().ConfigureAwait(false);
            }
        }

        private CallOptions GetCallOptions(bool isConnectToServer = false, bool addAuth = false,bool noDeadline = false)
        {
            int deadlineSec;
            CallOptions calloptions;
            if (isConnectToServer)
                deadlineSec = _coreComOptions.GrpcOptions.ConnectToServerDeadlineSec;
            else if (noDeadline || _coreComOptions.GrpcOptions.ConnectToServerDeadlineSec == -1)
            {
                deadlineSec = 0;
                Console.WriteLine("deadlineSec=NoDeadLine");
            }
            else
            {
                deadlineSec = _coreComOptions.GrpcOptions.MessageDeadlineSec * _coreComOptions.GrpcOptions.MessageDeadlineSecMultiplay;
                Console.WriteLine("deadlineSec=" + deadlineSec.ToString());

            }
            if (addAuth && !string.IsNullOrEmpty(_coreComOptions.ClientToken))
            {
                var headers = new Metadata();
                headers.Add("Authorization", $"Bearer {_coreComOptions.ClientToken}");
                calloptions = new CallOptions(headers);//.WithWaitForReady(true)
                if (!noDeadline)
                    calloptions = calloptions.WithDeadline(DateTime.UtcNow.AddSeconds(deadlineSec));
            }
            else
            {
                calloptions = new CallOptions();//.WithWaitForReady(true)
                if (!noDeadline)
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
                LogEventOccurred(new LogEvent { Description = response.Response, TransferStatus = TransferStatusEnum.Recived, MessageSize = response.CalculateSize() });
                Console.WriteLine("Connected to Server " + _coreComOptions.ServerAddress);

                ConnectionStatusChange(ConnectionStatusEnum.Connected);
                //Start timmer for check queue server and client
                if (_coreComOptions.GrpcOptions.RequestServerQueueIntervalSec > 0)
                    _checkQueueTimer.Enabled = true;

                LatestRpcExceptionChange(null);
              

                return true;
            }
            catch (RpcException ex)
            {
                LatestRpcExceptionChange(ex);

                ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                _timer.Enabled = true;

                return false;

            }
            catch (Exception ex)
            {
                ConnectionStatusChange(ConnectionStatusEnum.Disconnected);
                LogErrorOccurred(ex, new CoreComMessageResponse { ClientId = _coreComOptions.ClientId });
                _timer.Enabled = true;
                return false;
            }
        }
        
        private async Task<bool> ProcessQueue(bool noDeadline = false)
        {
            if (_workingOnQueue || _connectionStatus != ConnectionStatusEnum.Connected)
                return true;

            try
            {
                _workingOnQueue = true;
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
                            using var streamingCall = _coreComClient.SubscribeServerToClientAuth(item, GetCallOptions(false, item.SendAuth, noDeadline));

                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                await ParseServerToClientMessage(returnMessage);
                            }
                            if (item.TransferStatus == (int)TransferStatusEnum.New)
                            {
                                item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                item.TransferredUtc = Helpers.DateTimeConverter.DateTimeUtcNowToUnixTime();
                                LogEventOccurred(dbContext, item);
                            }
                            streamingCall.Dispose();
                        }
                        else
                        {
                            item.ClientId = _coreComOptions.ClientId.ToString();
                            LogEventOccurred(dbContext, item);
                            using var streamingCall = _coreComClient.SubscribeServerToClient(item, GetCallOptions(false, item.SendAuth, noDeadline));

                            //Now the outgoing messages is sent
                            await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync().ConfigureAwait(false))
                            {
                                await ParseServerToClientMessage(returnMessage);
                            }
                            if (item.TransferStatus == (int)TransferStatusEnum.New)
                            {
                                item.TransferStatus = (int)TransferStatusEnum.Transferred;
                                item.TransferredUtc = Helpers.DateTimeConverter.DateTimeUtcNowToUnixTime();
                                LogEventOccurred(dbContext, item);
                            }
                            streamingCall.Dispose();
                        }

                    }

                }



            }
            catch (RpcException ex)
            {
                _workingOnQueue = false;
                
                LatestRpcExceptionChange(ex);
                switch (ex.StatusCode)
                {
                    case StatusCode.DeadlineExceeded:
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        _coreComOptions.GrpcOptions.MessageDeadlineSecMultiplay = _coreComOptions.GrpcOptions.MessageDeadlineSecMultiplay * 2;
                        await ProcessQueue().ConfigureAwait(false);
                        return false;
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
                        Console.WriteLine("Unauthenticated.");
                        LogEventOccurred(new LogEvent { Description = ex.Message, ConnectionStatus = _connectionStatus });
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessageResponse { ClientId = _coreComOptions.ClientId });
                _workingOnQueue = false;
            }

            _workingOnQueue = false;
            //Start timmer for check Queue server and client
            if (_coreComOptions.GrpcOptions.RequestServerQueueIntervalSec > 0)
                _checkQueueTimer.Enabled = true;

            return true;
        }
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature, bool sendAuth, bool noDeadline)
        {
            string jsonObject = string.Empty;
            CoreComMessage coreComMessage;

            try
            {
                //Turn of timmer for message queue as we get the queue from this call
                _checkQueueTimer.Enabled = false;
                //error report to client
                if (outgoingObject != null)
                {
                    
                    jsonObject = JsonSerializer.Serialize(outgoingObject);
                }

                coreComMessage = new CoreComMessage
                {
                    CoreComMessageId = Guid.NewGuid().ToString(),
                    TransactionIdentifier = Guid.NewGuid().ToString(),
                    NewUtc = Helpers.DateTimeConverter.DateTimeUtcNowToUnixTime(),
                    MessageSignature = messageSignature,
                    JsonObject = jsonObject,
                    SendAuth = sendAuth
                };

                using (var dbContext = new CoreComContext(_dbContextOptions))
                {

                    dbContext.OutgoingMessages.Add(coreComMessage);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }


                await ProcessQueue(noDeadline).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessageResponse { ClientId = _coreComOptions.ClientId });
            }
            return true;
        }
        private async Task ParseServerToClientMessage(CoreComMessageResponse request)
        {

            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                request.TransferStatus = (int)TransferStatusEnum.Recived;
                request.RecivedUtc = Helpers.DateTimeConverter.DateTimeUtcNowToUnixTime();

                if (request.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue)
                {
                    await ParseCoreComFrameworkFromServerMessage(request).ConfigureAwait(false);
                    LogEventOccurred(dbContext, request);
                    return;
                }



                LogEventOccurred(dbContext, request);
                //System.Type type = Type.GetType(request.JsonObjectType);
                // erer = new CoreComMessagingCenter();
                Type type = CoreComMessagingCenter.GetMessageArgType(request.MessageSignature);
                if (type == null)
                {
                    CoreComMessagingCenter.Send(request.MessageSignature, null);
                }
                else
                {
                    var objectDeser = JsonSerializer.Deserialize(request.JsonObject, type);
                    CoreComMessagingCenter.Send(request.MessageSignature, null, objectDeser);
                }
                //CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId = request.ClientId };
                //if (string.IsNullOrEmpty(request.JsonObject))
                //{
                //    var funcToRun = _receiveDelegatesOneParm.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                //    if (funcToRun != null)
                //    {
                //        await funcToRun.Item1.Invoke(coreComUserInfo).ConfigureAwait(false);
                //    }
                //    else
                //    {
                //        LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
                //    }
                //}
                //else
                //{
                //    var funcToRun = _receiveDelegatesTwoParm.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                //    if (funcToRun != null)
                //    {
                //        var objectDeser = JsonSerializer.Deserialize(request.JsonObject, funcToRun.Item3);
                //        await funcToRun.Item1.Invoke(objectDeser, coreComUserInfo).ConfigureAwait(false);
                //    }
                //    else
                //    {
                //        LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
                //    }
                //}
            }
        }
        private async Task ParseCoreComFrameworkFromServerMessage(CoreComMessageResponse request)
        {

        }
        private async Task WriteIncommingMessagesLog(CoreComMessageResponse request)
        {
            if (_coreComOptions.LogSettings.LogMessageTarget != LogMessageTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = FileSystem.AppDataDirectory;// Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IncommingMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageResponseId + "\t" + request.MessageSignature + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t" + request.CalculateSize().ToString() + Environment.NewLine);
            }


        }
        private async Task WriteOutgoingMessagesLog(CoreComMessage request)
        {
            if (_coreComOptions.LogSettings.LogMessageTarget != LogMessageTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = FileSystem.AppDataDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "OutgoningMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.MessageSignature + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t" + request.CalculateSize().ToString() + Environment.NewLine);
            }


        }
        private async Task WriteEventLogtoFile(LogEvent logEvent)
        {
            if (_coreComOptions.LogSettings.LogEventTarget != LogEventTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = FileSystem.AppDataDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "LogEvents.log"), true))
            {
                await outputFile.WriteLineAsync(logEvent.TimeStampUtc.ToString() + "\t" + logEvent.LogEventId + "\t" + logEvent.Description + "\t" + logEvent.ClientId + "\t" + logEvent.TransactionIdentifier + "\t" + logEvent.TransferStatus.ToString() + "\t" + logEvent.MessageSize.ToString() + Environment.NewLine);
            }


        }
        private async Task WriteErrorLogtoFile(LogError logError)
        {
            if (_coreComOptions.LogSettings.LogErrorTarget != LogErrorTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = FileSystem.AppDataDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "LogError.log"), true))
            {
                await outputFile.WriteLineAsync(logError.TimeStampUtc.ToString() + "\t" + logError.LogErrorId + "\t" + logError.Description + "\t" + logError.ClientId + "\t" + logError.TransactionIdentifier + "\t" + logError.Stacktrace + Environment.NewLine);
            }


        }
        #endregion
        #region internal Event handlers
        internal async virtual void LogEventOccurred(CoreComContext dbContext, CoreComMessage coreComMessage)
        {

            LogEvent logEvent = new LogEvent { Description = coreComMessage.MessageSignature, TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus, MessageSize = coreComMessage.CalculateSize() };


            //Messages
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //allways remove CoreComInternal from outgoingmessage table
                    if (coreComMessage.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue
                        && coreComMessage.TransferStatus != (int)TransferStatusEnum.New)
                        dbContext.OutgoingMessages.Remove(coreComMessage);

                    //it's allready in db just update status

                    break;
                case LogMessageTargetEnum.TextFile:

                    await WriteOutgoingMessagesLog(coreComMessage);
                    break;
                case LogMessageTargetEnum.NoLoging:
                    if (coreComMessage.TransferStatus != (int)TransferStatusEnum.New)
                        dbContext.OutgoingMessages.Remove(coreComMessage);
                    break;
                default:
                    break;
            }

            //Events
            switch (_coreComOptions.LogSettings.LogEventTarget)
            {
                case LogEventTargetEnum.Database:
                    await dbContext.LogEvents.AddAsync(logEvent);

                    break;
                case LogEventTargetEnum.TextFile:

                    await WriteEventLogtoFile(logEvent);
                    break;
                case LogEventTargetEnum.NoLoging:

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
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //allways remove CoreComInternal from incomming table/ the massage is new so it does not exist in table
                    if (coreComMessageResponse.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                    { //add incomming message to db
                        coreComMessageResponse.CoreComMessageResponseId = Guid.NewGuid().ToString();
                        dbContext.IncomingMessages.Add(coreComMessageResponse);
                    }
                    break;
                case LogMessageTargetEnum.TextFile:
                    if (coreComMessageResponse.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                    { //add incomming message to file
                        await WriteIncommingMessagesLog(coreComMessageResponse);
                    }
                    break;
                case LogMessageTargetEnum.NoLoging:
                    //dbContext.OutgoingMessages.Remove(coreComMessage);
                    break;
                default:
                    break;
            }

            //Events
            switch (_coreComOptions.LogSettings.LogEventTarget)
            {
                case LogEventTargetEnum.Database:
                    if (coreComMessageResponse.TransferStatus != (int)TransferStatusEnum.New)
                        await dbContext.LogEvents.AddAsync(logEvent);

                    break;
                case LogEventTargetEnum.TextFile:
                    await WriteEventLogtoFile(logEvent);

                    break;
                case LogEventTargetEnum.NoLoging:

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
                switch (_coreComOptions.LogSettings.LogEventTarget)
                {
                    case LogEventTargetEnum.Database:
                        await dbContext.LogEvents.AddAsync(logEvent);

                        break;
                    case LogEventTargetEnum.TextFile:
                        await WriteEventLogtoFile(logEvent);

                        break;
                    case LogEventTargetEnum.NoLoging:

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
        internal async virtual void LogErrorOccurred(Exception exception, CoreComMessageResponse coreComMessage)
        {
            LogErrorOccurred(exception.Message, coreComMessage, exception);
        }
        internal async virtual void LogErrorOccurred(string description, CoreComMessageResponse coreComMessage, Exception ex = null)
        {

            LogError logError = new LogError { Description = description };

            if (coreComMessage != null)
            {
                logError.ClientId = coreComMessage.ClientId;
                logError.TransactionIdentifier = coreComMessage.TransactionIdentifier;
                if (string.IsNullOrEmpty(description))
                    logError.Description = coreComMessage.MessageSignature;

            }
            if (ex != null)
                logError.Stacktrace = ex.StackTrace;

            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                //Messages
                switch (_coreComOptions.LogSettings.LogErrorTarget)
                {
                    case LogErrorTargetEnum.Database:
                        dbContext.LogErros.Add(logError);
                        break;
                    case LogErrorTargetEnum.TextFile:
                        //Create textfile log
                        // await WriteErrorLogtoFile(logError).ConfigureAwait(false);
                        break;
                    case LogErrorTargetEnum.NoLoging:
                        //if (coreComMessage.TransferStatus != (int)TransferStatusEnum.New)
                        //    dbContext.IncomingMessages.Remove(coreComMessage);
                        break;
                    default:
                        break;
                }



                await dbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            EventHandler<LogError> handler = OnLogErrorOccurred;
            if (handler != null)
            {
                handler(this, logError);
            }
        }

        protected virtual void ConnectionStatusChange(ConnectionStatusEnum e)
        {
            _connectionStatus = e;
            EventHandler<ConnectionStatusEnum> handler = OnConnectionStatusChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void LatestRpcExceptionChange(RpcException e)
        {

            EventHandler<RpcException> handler = OnLatestRpcExceptionChange;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion
    }
}
