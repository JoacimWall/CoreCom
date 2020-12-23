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
        public CoreComService(IConfiguration config)
        {
            _coreComOptions = new CoreComOptions();

            _dbContextOptions = new DbContextOptionsBuilder<CoreComContext>()
                    .UseInMemoryDatabase(databaseName: "CoreComDb").Options;

            _config = config;
            _coreComOptions.LogSettings.logSource = (logSource)Convert.ToInt32(_config["CoreCom:CoreComOptions:LogSettings_logSource"]);

            //  services.AddDbContext<CoreComContext>(options => options.UseInMemoryDatabase(databaseName: "CoreComDb"));

            //using (var context = new CoreComContext(_dbContextOptions))
            //{
            //  //  context.Database.EnsureCreated();

            //    var result = context.TransferStatus.ToList();


            //}

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

            return new ConnectToServerResponse { Response = "Client are connected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };
        }
        public override async Task<DisconnectFromServerResponse> ClientDisconnectFromServer(DisconnectFromServerRequest request, ServerCallContext context)
        {
            RemoveClient(request.ClientId);
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
                //First process messages so it's added to cure
                if (isAuth)
                    await ParseClientToServerMessageAuth(request);
                else
                    await ParseClientToServerMessage(request);

                //Add loging
                request.TransferStatus = TransferStatusEnum.Recived;
                dbContext.IncomingMessages.Add(request);
                
                await dbContext.SaveChangesAsync();
            }
            //process cue
            await ProcessCue(request, responseStream, context);

            //Check if we allready got this meessage
            //var exist = await dbContext.OutgoingMessages.FirstOrDefaultAsync(x => x.TransactionIdentifier == request.TransactionIdentifier);
            //if (exist == null)
            //{

            //    //First process messages so it's added to cure
            //    if (isAuth)
            //        await ParseClientToServerMessageAuth(request);
            //    else
            //        await ParseClientToServerMessage(request);

            //    //Add loging
            //    request.TransferStatus = (int)TransferStatusEnum.Recived;
            //    dbContext.IncomingMessages.Add(request);
            //    //Add response to client
            //    dbContext.OutgoingMessages.Add(new CoreComMessageResponse
            //    {
            //        ClientId = request.ClientId,
            //        CoreComMessageResponseId = Guid.NewGuid().ToString(),
            //        TransactionIdentifier = request.TransactionIdentifier,
            //        MessageSignature = CoreComInternalSignatures.CoreComInternal_StatusUpdate,
            //        TransferStatus = request.TransferStatus
            //    });

            //    await dbContext.SaveChangesAsync();
            //}
            //else
            //{ //message exist allready then the client need to get status update that we have recived it
            //    //Change back from Transferd to Recived
            //    exist.TransferStatus = (int)TransferStatusEnum.Recived;
            //    await dbContext.SaveChangesAsync();
            //}




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
                using (var DbContext = new CoreComContext(_dbContextOptions))
                {
                        var outgoingMess = DbContext.OutgoingMessages.
                            Where(x => x.ClientId == request.ClientId &&
                            x.TransferStatus < TransferStatusEnum.Transferred).ToList();

                        foreach (var item in outgoingMess)
                        {
                            if (context.Deadline > DateTime.UtcNow)
                            {
                                //send
                                await responseStream.WriteAsync(item);
                                //logging
                                await WriteOutgoingMessagesLog(item);
                                //Remove messages
                                item.TransferStatus = TransferStatusEnum.Transferred;
                                await DbContext.SaveChangesAsync();
                            }
                      
                        }
                }
            }
            catch (RpcException ex)
            {
                
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
            //Logg messages
            await WriteIncommingMessagesLog(request);


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
            if (request.MessageSignature.StartsWith("CoreComInternal"))
            {
                await ParseCoreComFrameworkMessage(request);
                return;
            }
            //Logg messages
            await WriteIncommingMessagesLog(request);


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

        }

        private async Task WriteIncommingMessagesLog(CoreComMessage request)
        {
            if (_coreComOptions.LogSettings.logSource != logSource.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IncommingMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.ClientId + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + request.MessageSignature);
            }


        }
        private async Task WriteOutgoingMessagesLog(CoreComMessageResponse request)
        {
            if (_coreComOptions.LogSettings.logSource != logSource.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "OutgoningMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageResponseId + "\t" + request.ClientId + "\t" + request.TransactionIdentifier + "\t" + request.ClientId + "\t" + request.MessageSignature);
            }


        }
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            string jsonObjectType = string.Empty;
            string jsonObject = string.Empty;
            CoreComMessageResponse coreComMessage;
            Client client;
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
                    TransactionIdentifier = Guid.NewGuid().ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    dbContext.OutgoingMessages.Add(coreComMessage);
                    await dbContext.SaveChangesAsync();

                }

           
            }
            catch (Exception ex)
            {
                return false;
            }


            return true;



        }


        #endregion
    }
}
