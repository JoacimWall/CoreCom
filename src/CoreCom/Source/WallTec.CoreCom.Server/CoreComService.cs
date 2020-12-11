using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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

namespace WallTec.CoreCom.Server
{
    public class CoreComService : Proto.CoreCom.CoreComBase
    {
        public List<Client> Clients = new List<Client>();

        private List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, System.Type>>();

        //private Func<CoreComMessage, Task> _actionClientToServerMessage;

       
        private CoreComOptions _coreComOptions;
        private IConfiguration _config;
        public CoreComService(IConfiguration config)
        {
            _coreComOptions = new CoreComOptions();
           

            _config = config;
            _coreComOptions.LogSettings.logSource = (logSource)Convert.ToInt32(_config["CoreCome:CoreComOptions:LogSettings_logSource"]);
           
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


        public override async Task<ConnectToServerResponse> ClientConnectToServer(ConnectToServerRequest request, ServerCallContext context)
        {
            return new ConnectToServerResponse { Response = "Hi you are connected", ServerDateTime = Timestamp.FromDateTime(DateTime.UtcNow) };
        }
        public override async Task SubscribeServerToClient(CoreComMessage request, IServerStreamWriter<CoreComMessage> responseStream, ServerCallContext context)
        {
            AddClient(request);
            var client = Clients.FirstOrDefault(x => x.CoreComUserInfo.ClientInstallId == request.ClientInstallId);

            await ParseClientToServerMessage(request);

            if (client.ClientIsSending)
            {
                //Exit
            }

            
            while (!context.CancellationToken.IsCancellationRequested && client.ServerToClientMessages.Count > 0)
            {
                client.ClientIsSending = true;

                //Send message
                //await client.SendCue();

                //send old messages 
                while (client.ServerToClientMessages.Count > 0)
                {
                    try
                    {   //send
                        await responseStream.WriteAsync(client.ServerToClientMessages[0]);
                        //logging
                        await WriteOutgoingMessagesLog(client.ServerToClientMessages[0]);
                        //Remove messages
                        client.ServerToClientMessages.RemoveAt(0);
                    }
                    catch
                    {
                        //TODO:Add timer to send
                        //Reconnect
                        //if (!_isConnecting)
                        //{
                        //    IsOnline = false;
                        //    _timer.Enabled = true;
                        //}
                        //return false;
                    }
                }


            }

        }

        //public override async Task<CoreComMessage> ClientToServerCoreComMessage(CoreComMessage request, ServerCallContext context)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(request.MessageSignature))
        //        {
        //            await ParseClientToServerMessage(request);



        //        }

        //    }
        //    catch (IOException ex)
        //    {
        //        return new CoreComMessage();
        //    }
        //    return new CoreComMessage();
        //}
        //public override async Task<CoreComMessageResponse> ClientToServerCoreComMessage(CoreComMessage request, ServerCallContext context)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(request.MessageSignature))
        //        {
        //            await ParseClientToServerMessage(request);



        //        }

        //    }
        //    catch (IOException ex)
        //    {
        //        return new CoreComMessageResponse();
        //    }
        //    return new CoreComMessageResponse();
        //}
        #region "private functions"
       
        // private Func<CoreComMessage, Task> _actionLogOutgoingMessage;

        
        internal bool AddClient(CoreComMessage coreComMessage) //, IAsyncStreamWriter<CoreComMessage> stream
        {
            if (!Clients.Any(c => c.CoreComUserInfo.ClientInstallId == coreComMessage.ClientInstallId))
            {
                Console.WriteLine("Client connected for duplex messages" + coreComMessage.ClientInstallId);
                Clients.Add(new Client { CoreComUserInfo = new CoreComUserInfo { ClientInstallId = coreComMessage.ClientInstallId } }); //, Stream = stream
            }
            else
            {   //reconnect
                Console.WriteLine("Client reconnected for duplex messages" + coreComMessage.ClientInstallId);
                var client = Clients.FirstOrDefault(c => c.CoreComUserInfo.ClientInstallId == coreComMessage.ClientInstallId);
                // client.Stream = stream;

            }
            return true;
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


            CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientInstallId = request.ClientInstallId, ClientId = "" };
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
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.TransactionId + "\t" + request.ClientInstallId + "\t" + request.ClientId + "\t" + request.MessageSignature);
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
                await outputFile.WriteLineAsync(DateTime.UtcNow.ToString() + "\t" + request.TransactionId + "\t" + request.ClientInstallId + "\t" + request.ClientId + "\t" + request.MessageSignature);
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
                    TransactionId = Guid.NewGuid().ToString(),
                    ClientInstallId = coreComUserInfo.ClientInstallId,
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };

                client = Clients.First(x => x.CoreComUserInfo.ClientInstallId == coreComUserInfo.ClientInstallId);
                client.ServerToClientMessages.Add(coreComMessage);

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
