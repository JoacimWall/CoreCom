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
            _coreComOptions.LogSettings.logSource = (logSource)Convert.ToInt32(_config["CoreCome:CoreComOptions:LogSettings_logSource"]);

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
            //AddClient(request.ClientId);

            return new ConnectToServerResponse { Response = "Client are connected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };
        }
        public override async Task SubscribeServerToClient(CoreComMessage request, IServerStreamWriter<CoreComMessage> responseStream, ServerCallContext context)
        {
            //Client client= null;
            //First process messages so it's added to cure 
            await ParseClientToServerMessage(request);

            await ProcessCue(request, responseStream, context);
            ////we have null ifall server restarted while client was connected
            //if (!Clients.Any(x => x.CoreComUserInfo.ClientId == request.ClientId))
            //    client= AddClient(request.ClientId);

            //if (client.ClientIsSending)
            //{
            //    //Exit
            //}
            //client.ClientIsSending = true;

            //using (var DbContext = new CoreComContext(_dbContextOptions))
            //{
            //    var outgoingMess = DbContext.OutgoingMessages.ToList();
            ////send old messages 
            //while (context.Deadline > DateTime.Now && outgoingMess.Count > 0)
            //{
            //    try
            //    {   //send
            //        await responseStream.WriteAsync(outgoingMess[0]);
            //        //logging
            //        await WriteOutgoingMessagesLog(outgoingMess[0]);
            //        //Remove messages
            //        //TODO:Update status in db
            //        //client.ServerToClientMessages.RemoveAt(0);
            //    }
            //    catch
            //    {
            //        //TODO:Add timer to send
            //        //Reconnect
            //        //if (!_isConnecting)
            //        //{
            //        //    IsOnline = false;
            //        //    _timer.Enabled = true;
            //        //}
            //        //return false;
            //    }
            //}
            //}
            //client.ClientIsSending = false;
        }

       

        //Authenticated functions
        [Authorize]
        public override async Task SubscribeServerToClientAuth(CoreComMessage request, IServerStreamWriter<CoreComMessage> responseStream, ServerCallContext context)
        {
            
            //Check if the message has a status update in ServerToClientMessages then
            //we allready have reviced this before but the status update from server to client has not worked
            //var statusResponseExist = Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId)?.ServerToClientMessages.Any(x => x.TransactionId == request.TransactionId);
            //if (statusResponseExist != null && statusResponseExist.Value)
            //{

            //}
            //else
            //{
                //First process messages so it's added to cure 
                await ParseClientToServerMessageAuth(request);
            // }
            //get cue

            await ProcessCue(request, responseStream, context);

            //if (client.ClientIsSending)
            //{
            //    //Exit
            //}

            //Change status to Transferred if  it's not done before
            //if (statusResponseExist != null && !statusResponseExist.Value)
            //{
            //    client.ServerToClientMessages.Add(new CoreComMessage
            //    {
            //        ClientId = request.ClientId,
            //        MessageSignature = CoreComInternalSignatures.CoreComInternal_StatusUpdate,
            //        TransactionId = request.TransactionId,
            //        Status = (int)TransferStatusEnum.Transferred
            //    });

            //}
            
            //Test deadline
            //await Task.Delay(25000);
            //client.ClientIsSending = true;

            //send Status update messages
            //var statusUpdate = client.ServerToClientMessages.Where(x => x.Status == (int)TransferStatusEnum.Transferred).ToList();
            //while (context.Deadline > DateTime.Now && statusUpdate.Count > 0)
            //{
            //    try
            //    {   //send status update
            //        await responseStream.WriteAsync(statusUpdate[0]);
            //        //statusUpdate[0].Status = TransferStatus.Done;
            //        //logging
            //        //await WriteOutgoingMessagesLog(client.ServerToClientMessages[0]);
            //        //Remove messages
            //        //statusUpdate.RemoveAt(0);
            //    }
            //    catch
            //    {

            //    }
            //}


            //send old messages 
            //while (context.Deadline > DateTime.Now && client.ServerToClientMessages.Count > 0)
            //{


            //    try
            //    {   //send
            //        await responseStream.WriteAsync(client.ServerToClientMessages[0]);
            //        //logging
            //        await WriteOutgoingMessagesLog(client.ServerToClientMessages[0]);
            //        //Remove messages
            //        client.ServerToClientMessages.RemoveAt(0);
            //    }
            //    catch
            //    {
                    
            //    }
            //}


            //client.ClientIsSending = false;
        }


        #region "private functions"
        private async Task<bool> ProcessCue(CoreComMessage request, IServerStreamWriter<CoreComMessage> responseStream, ServerCallContext context)
        {
            //we have null ifall server restarted while client was connected
            if (!Clients.Any(x => x.CoreComUserInfo.ClientId == request.ClientId))
                 AddClient(request.ClientId);

            //if we are sending allready
            if (Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending)
                return true;


            Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending = true;
            try
            {

            
            using (var DbContext = new CoreComContext(_dbContextOptions))
            {
                var outgoingMess = DbContext.OutgoingMessages.ToList();
                while (context.Deadline > DateTime.Now && outgoingMess.Count > 0)
                {
                    try
                    {   //send
                        await responseStream.WriteAsync(outgoingMess[0]);
                        //logging
                        await WriteOutgoingMessagesLog(outgoingMess[0]);
                            //Remove messages
                            outgoingMess.RemoveAt(0);
                    }
                    catch
                    {

                    }
                }
            }
            }
            catch (Exception ex)
            {

                throw;
            }
            Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientId == request.ClientId).ClientIsSending = false;
           

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
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.ClientId + "\t" + request.ClientId + "\t" + request.MessageSignature);
            }


        }
        private async Task WriteOutgoingMessagesLog(CoreComMessage request)
        {
            if (_coreComOptions.LogSettings.logSource != logSource.TextFile)
                return;

            // Set a variable to the Documents path.
            string docPath = Environment.CurrentDirectory;

            // Write the specified text asynchronously to a new file named "WriteTextAsync.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "OutgoningMessages.log"), true))
            {
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.CoreComMessageId + "\t" + request.ClientId + "\t" + request.ClientId + "\t" + request.MessageSignature);
            }


        }
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature, CoreComUserInfo coreComUserInfo)
        {
            string jsonObjectType = string.Empty;
            string jsonObject = string.Empty;
            CoreComMessage coreComMessage;
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

                coreComMessage = new CoreComMessage
                {
                    CoreComMessageId = Guid.NewGuid().ToString(),
                    ClientId = coreComUserInfo.ClientId,
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };
                using (var dbContext = new CoreComContext(_dbContextOptions))
                {
                    dbContext.OutgoingMessages.Add(coreComMessage);
                    await dbContext.SaveChangesAsync();

                }

                //    client = Clients.First(x => x.CoreComUserInfo.ClientId == coreComUserInfo.ClientId);
                //client.ServerToClientMessages.Add(coreComMessage);

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
