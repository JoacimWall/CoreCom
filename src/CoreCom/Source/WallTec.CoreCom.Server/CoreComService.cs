using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public List<Client> Clients = new List<Client>();
        //Public functions needs no token
        private List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>>();
        //Authenticated functions
        private List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParmAuth = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>> _receiveDelegatesTwoParmAuth = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>>();

        private CoreComOptions _coreComOptions;
        private IConfiguration _config;
        private DbContextOptions _dbContextOptions;

        //events
        public event EventHandler<LogEvent> OnLogEventOccurred;

        public CoreComService(IConfiguration config)
        {
            _coreComOptions = new CoreComOptions();

            _config = config;
            _coreComOptions.LogSettings.LogMessageTarget = (LogMessageTargetEnum)System.Enum.Parse(typeof(LogMessageTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogMessageTarget"]);
            _coreComOptions.LogSettings.LogEventTarget = (LogEventTargetEnum)System.Enum.Parse(typeof(LogEventTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogEventTarget"]);
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
                    .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CoreComDb;Trusted_Connection=True;MultipleActiveResultSets=true").Options;
                    break;   
                default:
                    break;
            }
            

          
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {

                var test = dbContext.TransferStatus.Where(x => x.TransferStatusId > 0); 
            }
        
            
            

        }



        public async Task<bool> SendAsync(object outgoingObject, string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            return await SendInternalAsync(outgoingObject, messageSignature, coreComUserInfo);
        }
        public async Task<bool> SendAsync(string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            return await SendInternalAsync(null, messageSignature, coreComUserInfo);
        }
        public void Register(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParm.Add(Tuple.Create(callback, messageSignature));
        }
        public void Register(Func<object, CoreComUserInfo, Task> callback, string messageSignature, System.Type type)
        {
            _receiveDelegatesTwoParm.Add(Tuple.Create(callback, messageSignature, type));
        }
        public void RegisterAuth(Func<CoreComUserInfo, Task> callback, string messageSignature)
        {
            _receiveDelegatesOneParmAuth.Add(Tuple.Create(callback, messageSignature));
        }
        public void RegisterAuth(Func<object, CoreComUserInfo, Task> callback, string messageSignature, System.Type type)
        {
            _receiveDelegatesTwoParmAuth.Add(Tuple.Create(callback, messageSignature, type));
        }
        
        public override async Task<ConnectToServerResponse> ClientConnectToServer(ConnectToServerRequest request, ServerCallContext context)
        {
            //Add Client
            AddClient(request.ClientId);
            LogEventOccurred(new LogEvent { ClientId = request.ClientId,  Description = "Client are connected" });

            return new ConnectToServerResponse { Response = "Client are connected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };

        }
        public override async Task<DisconnectFromServerResponse> ClientDisconnectFromServer(DisconnectFromServerRequest request, ServerCallContext context)
        {
            RemoveClient(request.ClientId);
            LogEventOccurred(new LogEvent { ClientId = request.ClientId, Description = "Client are disconnected" });

            return new DisconnectFromServerResponse { Response = "Client are disconnected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) } ;
        }

        public override async Task SubscribeServerToClient(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            
            await SubscribeServerToClientInternal(false,request, responseStream, context);
        }

        //Authenticated functions
        [Authorize]
        public override async Task SubscribeServerToClientAuth(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            await SubscribeServerToClientInternal(true, request, responseStream, context);
        }


        #region "private functions"
        private  async Task SubscribeServerToClientInternal(bool isAuth, CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
        {
            using (var dbContext = new CoreComContext(_dbContextOptions))
            {
                //Add loging
                request.TransferStatus = (int)TransferStatusEnum.Recived;
                LogEventOccurred(dbContext, request);

                //First process messages so it's added to cure
                if (isAuth)
                    await ParseClientToServerMessageAuth(request);
                else
                    await ParseClientToServerMessage(request);

                
                

            }
            //process cue
            await ProcessCue(request, responseStream, context);

            
        }
        private async Task<bool> ProcessCue(CoreComMessage request, IServerStreamWriter<CoreComMessageResponse> responseStream, ServerCallContext context)
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
                                await responseStream.WriteAsync(item);
                                //Remove messages
                                item.TransferStatus = (int)TransferStatusEnum.Transferred;
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
                LogEventOccurred(new LogEvent { Description = ex.Message });
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
            //Todo: remove memory cue 
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
            if (request.MessageSignature.StartsWith("CoreComInternal"))
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
                    //TODO:Report error
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
                    //TODO:Report error
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
                        //CreateTimeUtc = Timestamp.FromDateTime(DateTime.UtcNow),
                        TransactionIdentifier = Guid.NewGuid().ToString()
                    };
                    LogEventOccurred(dbContext, internalMess);
                 
                }
            }
        }

        private async Task WriteIncommingMessagesLog(CoreComMessage request)
        {
            if (_coreComOptions.LogSettings.LogMessageTarget != LogMessageTargetEnum.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IncommingMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.MessageSignature + "\t" + request.ClientId  + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t" + request.CalculateSize().ToString() + Environment.NewLine);
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
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageResponseId + "\t" + request.MessageSignature + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + ((TransferStatusEnum)request.TransferStatus).ToString() + "\t"  + request.CalculateSize().ToString() + Environment.NewLine);
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
                await outputFile.WriteLineAsync(logEvent.TimeStampUtc.ToString() + "\t" + logEvent.LogEventId + "\t" + logEvent.Description + "\t" + logEvent.ClientId + "\t" + logEvent.TransactionIdentifier  + "\t" + logEvent.TransferStatus.ToString() +  "\t" + logEvent.MessageSize.ToString() + Environment.NewLine);
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
                    //CreateTimeUtc = Timestamp.FromDateTime(DateTime.UtcNow),
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
                return false;
            }


            return true;



        }


        #endregion
        #region Internal functions
       
        internal async virtual void LogEventOccurred(CoreComContext dbContext, CoreComMessage coreComMessage)
        {

            LogEvent logEvent = new LogEvent { Description = coreComMessage.MessageSignature, ClientId = coreComMessage.ClientId
                , TransactionIdentifier = coreComMessage.TransactionIdentifier,
                TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus, MessageSize = coreComMessage.CalculateSize() };


            //Messages
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //allways remove CoreComInternal from IncomingMessages table
                    //if (coreComMessage.MessageSignature != CoreComInternalSignatures.CoreComInternal_PullQueue)
                        dbContext.IncomingMessages.Add(coreComMessage);
                    break;
                case LogMessageTargetEnum.TextFile:
                    //Create textfile log
                    await WriteIncommingMessagesLog(coreComMessage).ConfigureAwait(false);
                    break;
                case LogMessageTargetEnum.NoLoging:
                    //if (coreComMessage.TransferStatus != (int)TransferStatusEnum.New)
                    //    dbContext.IncomingMessages.Remove(coreComMessage);
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

            LogEvent logEvent = new LogEvent { Description = coreComMessageResponse.MessageSignature,
                ClientId = coreComMessageResponse.ClientId,TransactionIdentifier = coreComMessageResponse.TransactionIdentifier,
                TransferStatus = (TransferStatusEnum)coreComMessageResponse.TransferStatus, MessageSize = coreComMessageResponse.CalculateSize() };

            //allways add to new outgoing to database 
            if (coreComMessageResponse.TransferStatus == (int)TransferStatusEnum.New)
                dbContext.OutgoingMessages.Add(coreComMessageResponse);


            //Messages
            switch (_coreComOptions.LogSettings.LogMessageTarget)
            {
                case LogMessageTargetEnum.Database:
                    //status change will be writen to database on dbContext.SaveChangesAsync
                    break;
                case LogMessageTargetEnum.TextFile:
                    //Create textfile log
                     //add response message to file
                     await WriteOutgoingMessagesLog(coreComMessageResponse);
                    
                    break;
                case LogMessageTargetEnum.NoLoging: 
                    if (coreComMessageResponse.TransferStatus == (int)TransferStatusEnum.Transferred)
                    { 
                       //remove message
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
