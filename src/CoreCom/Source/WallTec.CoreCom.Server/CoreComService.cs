using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Server.Models;
using WallTec.CoreCom.Sheard;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Server
{
    public class CoreComService : Proto.CoreCom.CoreComBase
    {
        //Public
        public List<Client> Clients = new List<Client>();
        public event EventHandler<LogEvent> OnLogEventOccurred;
        public event EventHandler<LogError> OnLogErrorOccurred;
        //Private
        //Functions needs no token
        private List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>>();
        //functions authenticated 
        private List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParmAuth = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>> _receiveDelegatesTwoParmAuth = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>>();

        private CoreComOptions _coreComOptions;
        private IConfiguration _config;
        private DbContextOptions _dbContextOptions;



        public CoreComService(IConfiguration config)
        {
            _coreComOptions = new CoreComOptions();

            _config = config;
            try
            {


                _coreComOptions.LogSettings.LogMessageTarget = (LogMessageTargetEnum)System.Enum.Parse(typeof(LogMessageTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogMessageTarget"]);
                _coreComOptions.LogSettings.LogEventTarget = (LogEventTargetEnum)System.Enum.Parse(typeof(LogEventTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogEventTarget"]);
                _coreComOptions.LogSettings.LogErrorTarget = (LogErrorTargetEnum)System.Enum.Parse(typeof(LogErrorTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogErrorTarget"]);

                _coreComOptions.DatabaseMode = (DatabaseModeEnum)System.Enum.Parse(typeof(DatabaseModeEnum), _config["CoreCom:CoreComOptions:Database:DatabaseMode"]);

                string connectionstring = _config["CoreCom:CoreComOptions:Database:ConnectionString"];

                switch (_coreComOptions.DatabaseMode)
                {
                    case DatabaseModeEnum.UseImMemory:
                        _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                        .UseInMemoryDatabase(databaseName: "CoreComDb").Options;
                        break;
                    case DatabaseModeEnum.UseSqlite:
                        _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                        .UseSqlite(connectionstring).Options;
                        break;
                    case DatabaseModeEnum.UseSqlServer:
                        _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                        .UseSqlServer(connectionstring).Options;
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, null);

            }

        }


        /// <summary>
        /// Queue outging message that has object as payload
        /// </summary>
        /// <param name="outgoingObject"></param>
        /// <param name="messageSignature"></param>
        /// <param name="coreComUserInfo"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(object outgoingObject, string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, coreComUserInfo);
        }
        /// <summary>
        /// Queue outging message
        /// </summary>
        /// <param name="messageSignature"></param>
        /// <param name="coreComUserInfo"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            return await SendInternalAsync(null, messageSignature, coreComUserInfo);
        }
        /// <summary>
        /// Regsiter function that is trigerd by the provided Message signature. 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageSignature"></param>
        public void Register(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParm.Add(Tuple.Create(callback, messageSignature));
        }
        /// <summary>
        /// Regsiter function that is trigerd by the provided Message signature and expect object as payload
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageSignature"></param>
        /// <param name="type"></param>
        public void Register(Func<object, CoreComUserInfo, Task> callback, string messageSignature, System.Type type)
        {
            _receiveDelegatesTwoParm.Add(Tuple.Create(callback, messageSignature, type));
        }
        /// <summary>
        /// Regsiter function that is trigerd by the provided Message signature.
        /// Require that the user also has authenticated JWT token
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageSignature"></param>
        public void RegisterAuth(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParmAuth.Add(Tuple.Create(callback, messageSignature));
        }
        /// <summary>
        /// Regsiter function that is trigerd by the provided Message signature and expect object as payload.
        /// Require that the user also has authenticated JWT token
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="messageSignature"></param>
        /// <param name="type"></param>
        public void RegisterAuth(Func<object, CoreComUserInfo, Task> callback, string messageSignature, System.Type type)
        {
            _receiveDelegatesTwoParmAuth.Add(Tuple.Create(callback, messageSignature, type));
        }
        /// <summary>
        /// This is used by the framework dont use this from your own code
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<ConnectToServerResponse> ClientConnectToServer(ConnectToServerRequest request, ServerCallContext context)
        {
            try
            {
                //Add Client
                AddClient(request.ClientId);
                LogEventOccurred(new LogEvent { ClientId = request.ClientId, Description = "Client are connected" });

                return new ConnectToServerResponse { Response = "Client are connected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessage { ClientId = request.ClientId });
                return null;
            }
        }
        /// <summary>
        /// This is used by the framework dont use this from your own code
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<DisconnectFromServerResponse> ClientDisconnectFromServer(DisconnectFromServerRequest request, ServerCallContext context)
        {
            try
            {
                RemoveClient(request.ClientId);
                LogEventOccurred(new LogEvent { ClientId = request.ClientId, Description = "Client are disconnected" });

                return new DisconnectFromServerResponse { Response = "Client are disconnected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessage
                {
                    ClientId = request.ClientId
                });
                return null;
            }
        }
        /// <summary>
        /// This is used by the framework dont use this from your own code
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task SubscribeServerToClient(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {

            await SubscribeServerToClientInternal(false, request, responseStream, context);
        }
        /// <summary>
        /// This is used by the framework dont use this from your own code
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [Authorize] //Authenticated functions
        public override async Task SubscribeServerToClientAuth(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            await SubscribeServerToClientInternal(true, request, responseStream, context);
        }


        #region "private functions"
        private async Task SubscribeServerToClientInternal(bool isAuth, CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            try
            {
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    //Add loging
                    request.TransferStatus = (int)TransferStatusEnum.Recived;
                    request.RecivedUtc = Sheard.Helpers.DateTimeConverter.DateTimeUtcNow();

                    //Check if we alread has responde to this
                    if (!dbContext.IncomingMessages.Any(x => x.TransactionIdentifier == request.TransactionIdentifier))
                    {
                        //First process messages so it's added to cure
                        if (isAuth)
                            await ParseClientToServerMessageAuth(request);
                        else
                            await ParseClientToServerMessage(request);

                        //Logging
                        LogEventOccurred(dbContext, request);
                    }
                }
                //process cue
                await ProcessQueue(request, responseStream, context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessage
                {
                    ClientId = request.ClientId
                });

            }
        }

        private async Task<bool> ProcessQueue(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            //we have null ifall server restarted while client was connected
            if (!Clients.Any(x => x.CoreComUserInfo.ClientId == request.ClientId))
                AddClient(request.ClientId);

            //check if we are sending allready
            if (Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending)
                return true;


            Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending = true;

            try
            {
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    //We need always responde with a message other vise loggin on client not work
                    //and the same CoreComInternal_PullQueue from client will send and wo will have the same
                    //transactionId twice result in db error 
                    var outgoingMess = dbContext.OutgoingMessages.
                        Where(x => x.ClientId == request.ClientId &&
                        x.TransferStatus < (int)TransferStatusEnum.Transferred).ToList();


                    foreach (var item in outgoingMess)
                    {
                        if (context.Deadline > DateTime.UtcNow)
                        {
                            //send
                            //await Task.Delay(7000);
                            await responseStream.WriteAsync(item);
                            //update messages
                            item.TransferStatus = (int)TransferStatusEnum.Transferred;
                            item.TransferredUtc = Sheard.Helpers.DateTimeConverter.DateTimeUtcNow();
                            LogEventOccurred(dbContext, item);
                        }

                    }

                }
            }
            catch (RpcException ex)
            {
                LogEventOccurred(new LogEvent { Description = ex.Message });
                switch (ex.StatusCode)
                {
                    case StatusCode.DeadlineExceeded:
                        Console.WriteLine("DeadlineExceeded");
                        break;
                    case StatusCode.Cancelled:

                        Console.WriteLine("Stream cancelled.");
                        break;
                    case StatusCode.PermissionDenied:
                    case StatusCode.Unavailable:
                        Console.WriteLine("PermissionDenied/Unavailable");
                        break;
                    case StatusCode.Unauthenticated:
                        Console.WriteLine("Unauthenticated.");
                        break;
                    default:
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessage { ClientId = request.ClientId, MessageSignature = request.MessageSignature });

                return false;
            }
            finally
            {
                Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending = false;

            }
            return true;


        }
        internal bool RemoveClient(string clientId)
        {

            Clients.Remove(Clients.FirstOrDefault(c => c.CoreComUserInfo.ClientId == clientId));
            //Todo: remove memory Queue 
            return true;
        }

        internal Client AddClient(string clientId)
        {
            Client client;
            if (!Clients.Any(c => c.CoreComUserInfo.ClientId == clientId))
            {
                Console.WriteLine("Client connected" + clientId);
                client = new Client { CoreComUserInfo = new CoreComUserInfo { ClientId = clientId } };
                Clients.Add(client); //, Stream = stream
            }
            else
            {   //reconnect
                Console.WriteLine("Client reconnected" + clientId);
                client = Clients.FirstOrDefault(c => c.CoreComUserInfo.ClientId == clientId);
            }
            return client;
        }

        private async Task ParseClientToServerMessage(CoreComMessage request)
        {
            //this only hapend after first messages bin sent
            if (request.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue)
            {
                await ParseCoreComFrameworkMessage(request);
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
                    
                    LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
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
                    LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
                }
            }
        }
        private async Task ParseClientToServerMessageAuth(CoreComMessage request)
        {
            //this only hapend after first messages bin sent
            if (request.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue)
            {
                await ParseCoreComFrameworkMessage(request);
                return;
            }


            CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId = request.ClientId };
            if (string.IsNullOrEmpty(request.JsonObject))
            {
                var funcToRun = _receiveDelegatesOneParmAuth.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                if (funcToRun != null)
                {
                    await funcToRun.Item1.Invoke(coreComUserInfo);
                }
                else
                {
                    LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
                }
            }
            else
            {
                var funcToRun = _receiveDelegatesTwoParmAuth.FirstOrDefault(x => x.Item2 == request.MessageSignature);
                if (funcToRun != null)
                {
                    var objectDeser = JsonSerializer.Deserialize(request.JsonObject, funcToRun.Item3);
                    await funcToRun.Item1.Invoke(objectDeser, coreComUserInfo);
                }
                else
                {
                    LogErrorOccurred("No function mapped to " + request.MessageSignature, request);
                }
            }
        }
        private async Task ParseCoreComFrameworkMessage(CoreComMessage request)
        {
            //We need always responde with a message other vise loggin on client not work
            //and the same CoreComInternal_PullQueue from client will send and wo will have the same
            //transactionId twice result in db error
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                //queue is empty then create response message
                if (!dbContext.OutgoingMessages.Any(x => x.ClientId == request.ClientId && x.TransferStatus < (int)TransferStatusEnum.Transferred))
                {
                    //send
                    var internalMess = new CoreComMessageResponse
                    {
                        MessageSignature = CoreComInternalSignatures.CoreComInternal_PullQueue,
                        CoreComMessageResponseId = Guid.NewGuid().ToString(),
                        ClientId = request.ClientId,
                        NewUtc = Sheard.Helpers.DateTimeConverter.DateTimeUtcNow(),
                        TransactionIdentifier = Guid.NewGuid().ToString()
                    };
                    LogEventOccurred(dbContext, internalMess);

                }
            }
        }
        //TODO:Add date to filename
        private async Task WriteIncommingMessagesLog(CoreComMessage request)
        {
            if (_coreComOptions.LogSettings.LogMessageTarget != LogMessageTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IncommingMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.MessageSignature + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t" + request.CalculateSize().ToString() + Environment.NewLine);
            }


        }
        private async Task WriteOutgoingMessagesLog(CoreComMessageResponse request)
        {
            if (_coreComOptions.LogSettings.LogMessageTarget != LogMessageTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "OutgoningMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageResponseId + "\t" + request.MessageSignature + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t" + request.CalculateSize().ToString() + Environment.NewLine);
            }


        }
        private async Task WriteEventLogtoFile(LogEvent logEvent)
        {
            if (_coreComOptions.LogSettings.LogEventTarget != LogEventTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

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
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "LogError.log"), true))
            {
                await outputFile.WriteLineAsync(logError.TimeStampUtc.ToString() + "\t" + logError.LogErrorId + "\t" + logError.Description + "\t" + logError.ClientId + "\t" + logError.TransactionIdentifier + "\t" + logError.Stacktrace + Environment.NewLine);
            }


        }
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            string jsonObjectType = string.Empty;
            string jsonObject = string.Empty;
            CoreComMessageResponse coreComMessage;
            //Build  message
            try
            {
                //error report to client
                if (outgoingObject != null)
                {
                    jsonObjectType = outgoingObject.GetType().ToString();
                    jsonObject = JsonSerializer.Serialize(outgoingObject);
                }

                coreComMessage = new CoreComMessageResponse
                {
                    CoreComMessageResponseId = Guid.NewGuid().ToString(),
                    ClientId = coreComUserInfo.ClientId,
                    NewUtc = Sheard.Helpers.DateTimeConverter.DateTimeUtcNow(),
                    TransactionIdentifier = Guid.NewGuid().ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    LogEventOccurred(dbContext, coreComMessage);

                }


            }
            catch (Exception ex)
            {
                LogErrorOccurred(ex, new CoreComMessage { ClientId = coreComUserInfo.ClientId, MessageSignature = messageSignature });
                return false;
            }


            return true;



        }


        #endregion
        #region Internal functions
        internal async virtual void LogErrorOccurred(Exception exception, CoreComMessage coreComMessage)
        {
            LogErrorOccurred(exception.Message, coreComMessage, exception);
        }
        internal async virtual void LogErrorOccurred(string description, CoreComMessage coreComMessage, Exception ex = null)
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
                        //allways remove CoreComInternal from IncomingMessages table
                        //if (coreComMessage.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                        dbContext.LogErros.Add(logError);
                        break;
                    case LogErrorTargetEnum.TextFile:
                        //Create textfile log
                        await WriteErrorLogtoFile(logError).ConfigureAwait(false);
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
        internal async virtual void LogEventOccurred(CoreComContext dbContext, CoreComMessage coreComMessage)
        {

            LogEvent logEvent = new LogEvent
            {
                Description = coreComMessage.MessageSignature,
                ClientId = coreComMessage.ClientId,
                TransactionIdentifier = coreComMessage.TransactionIdentifier,
                TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus,
                MessageSize = coreComMessage.CalculateSize()
            };


            //Messages
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //allways remove CoreComInternal from IncomingMessages table
                    if (coreComMessage.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                    {
                        //the same message can get recived many times if deadline exced has happend
                        //its the TransactionIdentifier that connect them togheter
                        coreComMessage.CoreComMessageId = Guid.NewGuid().ToString();
                        dbContext.IncomingMessages.Add(coreComMessage);
                    }
                    break;
                case LogMessageTargetEnum.TextFile:
                    //Create textfile log
                    if (coreComMessage.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                    {
                        coreComMessage.CoreComMessageId = Guid.NewGuid().ToString();
                        await WriteIncommingMessagesLog(coreComMessage).ConfigureAwait(false);
                    }
                    break;
                case LogMessageTargetEnum.NoLoging:
                    //if (coreComMessage.TransferStatus != (int)TransferStatusEnum.Recived)
                    //{
                    //    dbContext.IncomingMessages.Remove(coreComMessage);
                    //}    
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
                    //Create textfile log
                    await WriteEventLogtoFile(logEvent).ConfigureAwait(false);
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

            LogEvent logEvent = new LogEvent
            {
                Description = coreComMessageResponse.MessageSignature,
                ClientId = coreComMessageResponse.ClientId,
                TransactionIdentifier = coreComMessageResponse.TransactionIdentifier,
                TransferStatus = (TransferStatusEnum)coreComMessageResponse.TransferStatus,
                MessageSize = coreComMessageResponse.CalculateSize()
            };

            //allways add to new outgoing to database 
            if (coreComMessageResponse.TransferStatus == (int)TransferStatusEnum.New)
                dbContext.OutgoingMessages.Add(coreComMessageResponse);


            //Messages
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //status change will be writen to database on dbContext.SaveChangesAsync
                    //we don't save Pullqueue messages
                    if (coreComMessageResponse.TransferStatus == (int)TransferStatusEnum.Transferred
                        && coreComMessageResponse.MessageSignature == CoreComInternalSignatures.CoreComInternal_PullQueue)
                    {   //remove message
                        dbContext.OutgoingMessages.Remove(coreComMessageResponse);
                    }
                    break;
                case LogMessageTargetEnum.TextFile:
                    //Create textfile log
                    //add response message to file
                    await WriteOutgoingMessagesLog(coreComMessageResponse);

                    break;
                case LogMessageTargetEnum.NoLoging:
                    if (coreComMessageResponse.TransferStatus == (int)TransferStatusEnum.Transferred)
                    {   //remove message
                        dbContext.OutgoingMessages.Remove(coreComMessageResponse);
                    }
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
                    //Create textfile log
                    await WriteEventLogtoFile(logEvent).ConfigureAwait(false);
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
                        //Create textfile log
                        await WriteEventLogtoFile(logEvent).ConfigureAwait(false);
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

        #endregion
    }
}
