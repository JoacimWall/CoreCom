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
    
    public class CoreComClient : INotifyPropertyChanged
    {
        #region Private Propertys
        //GRPC Propertys
        
        private HttpClientHandler _httpHandler;
        private GrpcChannel _channel;
        private CoreComOptions _coreComOptions;
        private Proto.CoreCom.CoreComClient _coreComClient;
        
        private CancellationToken _cancellationToken;
        private AsyncServerStreamingCall<CoreComMessage> _serverStream;
        private Task _responseTask; //keep this

        //Messages propertys
        Guid _clientInstallId;
        private readonly List<Tuple<Func<CoreComUserInfo, Task>, string>> _receiveDelegatesOneParm = new List<Tuple<Func<CoreComUserInfo, Task>, string>>();
        private readonly List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>> _receiveDelegatesTwoParm = new List<Tuple<Func<object, CoreComUserInfo, Task>, string, Type>>();
        private readonly List<CoreComMessage> _messagesOutgoing = new List<CoreComMessage>();
        
        //Offline Propertys
        bool _isConnecting;
        private System.Timers.Timer _timer;


        #endregion
        #region Public Propertys
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isOnline;
        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }

            set
            {
                if (value != _isOnline)
                {
                    _isOnline = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion
        #region Public Functions
        public CoreComClient()
        {
            // Create a timer with a ten second interval.
            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += OnConnectTimedEvent;
        }

        public bool Connect(CoreComOptions coreComOptions)
        {
            _coreComOptions = coreComOptions;
             //Xamarin
             //System.Guid? installId = await AppCenter.GetInstallIdAsync(); 
             _clientInstallId = Guid.NewGuid();

            _timer.Interval = 2000;
            _timer.Enabled = true;

            return true;
        }
        public async Task<bool> ShutdownAsync()
        {
            //await _duplexStream.RequestStream.CompleteAsync();
            await _channel.ShutdownAsync();

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
            return await SendInternalAsync(outgoingObject, messageSignature);
        }
        public async Task<bool> SendAsync(string messageSignature)
        {
            return await SendInternalAsync(null, messageSignature);
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
            //First time 2 sec
            if (_timer.Interval != Convert.ToDouble(2000))
            {   //after connection error wait 20 sec to try
                _timer.Interval = 20000;
            }
            await OpenChannel();

        }
        private async Task<bool> OpenChannel()
        {
            try
            {
                if (_isConnecting)
                    return false;

                _isConnecting = true;
                IsOnline = false;

                _httpHandler = new HttpClientHandler();
                if (_coreComOptions.ClientIsMobile)
                {  
                    //case Device.Android:
                    //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    _httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                _channel = GrpcChannel.ForAddress($"{_coreComOptions.ServerAddress}", new GrpcChannelOptions
                {
                    HttpHandler = new GrpcWebHandler(_httpHandler)
                });

                CallOptions calloptions = new CallOptions().WithWaitForReady(true);
                calloptions = calloptions.WithDeadline(DateTime.UtcNow.AddSeconds(20));

                _coreComClient = new Proto.CoreCom.CoreComClient(_channel);
                
                //Wait for channel to open for 20 sec
                var response = await _coreComClient.ClientConnectToServerAsync( new ConnectToServerRequest { ClientInstallId = _clientInstallId.ToString() }, calloptions);
                
                Console.WriteLine("Connected to Server " + _coreComOptions.ServerAddress);
                _isConnecting = false;
                IsOnline = true;
                //send current cue
               // var result = await SendCue();

                return true;
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.DeadlineExceeded)
                {
                    IsOnline = false;
                    _isConnecting = false;
                    _timer.Enabled = true;

                }

                return false;

            }
            catch (Exception ex)
            {
                IsOnline = false;
                _timer.Enabled = true;
                return false;
            }
            finally
            {
                _isConnecting = false;

            }

        }
        
        private async Task<bool> SendInternalAsync(object outgoingObject, string messageSignature)
        {
            string jsonObjectType = string.Empty;
            string jsonObject= string.Empty;
            CoreComMessage coreComMessage;
            //Build  message
            try
            {
                //error reort to client
                if (outgoingObject != null)
                {
                    jsonObjectType = outgoingObject.GetType().ToString();
                    jsonObject = JsonSerializer.Serialize(outgoingObject);
                }
                
                coreComMessage = new CoreComMessage
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    ClientInstallId = _clientInstallId.ToString(),
                    MessageSignature = messageSignature,
                    JsonObjectType = jsonObjectType,
                    JsonObject = jsonObject
                };


           
                 _messagesOutgoing.Add(coreComMessage);
                
               
                //TODO: token 100 sec?
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(100));
                while (_messagesOutgoing.Count > 0 && !_isConnecting)
                {
                   
                   
                    using var streamingCall = _coreComClient.SubscribeServerToClient(_messagesOutgoing[0], cancellationToken: cts.Token);
                    _messagesOutgoing.RemoveAt(0);
                    try
                    {
                        await foreach (var returnMessage in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
                        {
                           await ParseServerToClientMessage(returnMessage);
                        }
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                    {
                        Console.WriteLine("Stream cancelled.");
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            //Send message
            //await SendCue();

            return true;
        }
        //private async Task<bool> SendCue()
        //{
        //    //send old messages 
        //    while (_messagesOutgoing.Count > 0 && !_isConnecting)
        //    {
        //        try
        //        {
        //           var result = _coreComClient.ClientToServerCoreComMessageAsync(_messagesOutgoing[0]);
        //            _messagesOutgoing.RemoveAt(0);
        //        }
        //        catch (Exception ex)
        //        {
        //            //Reconnect
        //            if (!_isConnecting)
        //            {
        //                IsOnline = false;
        //                _timer.Enabled = true;
        //            }
        //            return false;
        //        }
        //    }
        //   // await GetCue();

        //    return true;
        //}
        //private async Task<bool> GetCue()
        //{

        //    if (_serverStream == null)
        //        _serverStream = _coreComClient.SubscribeServerToClient(new CoreComMessage { ClientInstallId = _clientInstallId.ToString(), MessageSignature = CoreComInternalSignatures.CoreComInternal_ConnectInstallId });

        //    while (await _serverStream.ResponseStream.MoveNext())
        //    {
        //        //Console.WriteLine("Greeting: " + call.ResponseStream.Current.Message);
        //        await ParseServerToClientMessage(_serverStream.ResponseStream.Current);

        //    }

        //    return true;

        //}
        private async Task ParseServerToClientMessage(CoreComMessage request)
        {
            if (request.MessageSignature.StartsWith("CoreComInternal"))
            {
                await ParseCoreComFrameworkFromServerMessage(request);
                return;
            }


            CoreComUserInfo coreComUserInfo = new CoreComUserInfo { ClientId = "", ClientInstallId = request.ClientInstallId };
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
        //private async Task HandleResponsesAsync(CancellationToken token)
        //{

        //    try
        //    {
        //        //if (_serverStream == null)
        //        var callOptions = new CallOptions();
        //        CallOptions calloptions = new CallOptions().WithWaitForReady(true);
        //        calloptions = calloptions.WithDeadline(DateTime.UtcNow.AddSeconds(80));
        //        using var  serverStream = _coreComClient.SubscribeServerToClient(new CoreComMessage { ClientInstallId = _clientInstallId.ToString(), MessageSignature = CoreComInternalSignatures.CoreComInternal_ConnectInstallId },callOptions);
                
                
        //            while (await serverStream.ResponseStream.MoveNext())
        //            {
        //                Console.WriteLine("Incoming message: " + serverStream.ResponseStream.Current.MessageSignature);
        //                await ParseServerToClientMessage(serverStream.ResponseStream.Current);


        //            }
                
        //    }
        //    catch (Exception ex)
        //    {
        //        //Reconnect
        //        if (!_isConnecting)
        //        {
        //            IsOnline = false;
        //            _timer.Enabled = true;
        //        }
        //    }
        //    //Detta kräver .net standard 2.1 and Grpc.Net.Common
        //    //await foreach (var update in stream.ReadAllAsync(token))
        //    //{
        //    //    Debug.WriteLine(update.MessageSignature);
        //    //}
        //}

        #endregion
    }
}
