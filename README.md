# CoreCom
Framework for wrapping gRPC to be used in project that targets .Net standard (Xamarin) or NetCore 5.0 on the client side and .Net Core 50 or 3.1 on server side.
To use this please use the nuget's WallTec.CoreCom.Client and WallTec.CoreCom.Server throw nuget manager in Visual studio. The solution build on top of grpc-web and are able to be hosted in azure or any other hosting that support ASP.NET Core. 

The framework wrapping the Proto files and gRPC logic so the client only register functions that listen to diffrent messages on the server and client side. Please view the sample for more information.       

### Offline suport
The framework takes care of any connection or transmission errors and queues them to make a new transmission when connection is restored.  

### Detailed logging
Detailed logging can be turned on. All transactions are written to the database or text file.

## Instructions server implementation
Project support .Net Core 5.0 or .NetCore 3.1  

Step 1,Create gRPC service project and Install NuGet Package: 
Select the template gRPC Service and create then add 
WallTec.CoreCom.Server in the .Net Core 5.0 or .NetCore 3.1 project.    

Step 2: Add the settings sections into the Appsettings.json file  
If you are going to debug this on a Mac please add the section Kestrel to the appsettings.Development.json
and change the the "Protocols": "Http1"
For more information about the diffrent settings read documentation below or view the sample code. 
```csharp
{
  .....
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http2"
  }
  },
  "CoreCom": {
    "CoreComOptions": {
      "LogSettings": {
        "LogMessageTarget": "Database",
        "LogMessageHistoryDays": "10",
        "LogEventTarget": "NoLoging",
        "LogEventHistoryDays": "10",
        "LogErrorTarget": "NoLoging",
        "LogErrorHistoryDays": "10"
      },
      "Database": {
        "DatabaseMode": "UseSqlite",
        "ConnectionString": "Data Source=CoreComDb.db"
      }
    }
  } 
}
``` 
Step 3: Create a service to handle the CoreCom implementation.  
 
```csharp
interface IMyService
{
}
public class MyService : IMyService
{
    private CoreComService _coreComService;
    private List<Projecs> _fakeDb = new List<Projecs>;
    public MyService(CoreComService coreComService)
    {
        _coreComService = coreComService;
        //This public
        _coreComService.Register(CoreComSignatures.RequestAllProjects, GetAllProjectsFromDb);
        //This need that the have a token
        _coreComService.RegisterAuth<Project>(CoreComSignatures.AddProject, AddProjectsToDb);
        _coreComService.RegisterAuth<Project>(CoreComSignatures.DeleteProject, DeleteProject);
    }    
    
    private async void AddProjectsToDb(Project value,CoreComUserInfo arg)
    {
        //Validate input
        if (string.IsNullOrEmpty(value.Name))
        {
            var error = new Result<Project>("The project need a name");
            await _coreComService.SendAsync(error, CoreComSignatures.AddProject, arg);
            return;
        }
        _fakeDb.Add(value );
        //send the new projet to all client that are connected 
        foreach (var item in _coreComService.Clients)
        {
            await _coreComService.SendAsync(value, CoreComSignatures.AddProject, new CoreComUserInfo { ClientId = item.CoreComUserInfo.ClientId });
        }

     }
     private async void GetAllProjectsFromDb(CoreComUserInfo coreComUserInfo)
    {
        await _coreComService.SendAsync(_fakeDb, CoreComSignatures.ResponseAllProjects, coreComUserInfo);
     }
}       
``` 
Step 4: Modify the Startup.cs   
```csharp
public class Startup
{
    //This two lines is if you would like to AddAuthorization
    private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
    private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
    
    public void ConfigureServices(IServiceCollection services)
    {   //this so you would be able to send large messags like images and more.
        services.AddGrpc(options => 
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = null; //When set to null, the message size is unlimited. or 2 * 1024 * 1024; // 2 MB
            options.MaxSendMessageSize = null; //When set to null, the message size is unlimited.
        });
            
         //This two is needed for the CoreCom
        services.AddSingleton<CoreComService>();
        services.AddHostedService<CoreComBackgroundService>();
        //Your service
        services.AddSingleton<IMyService,MyService>();

        //This is if you would like to AddAuthorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.Name);
            });
        });
        //This is if you would like to AddAuthorization
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateActor = false,
                        ValidateLifetime = true,
                        IssuerSigningKey = SecurityKey
                    };
            });
        }
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //Warm it up to register handlers 
        app.ApplicationServices.GetService<IMyService>();

        app.UseRouting();
        //This two lines is if you would like to AddAuthorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        //This is for the gRPC framwork to work  
        app.UseGrpcWeb();


        app.UseEndpoints(endpoints =>
        {
            //Register the CoreCom framework 
            endpoints.MapGrpcService<CoreComService>().EnableGrpcWeb();
            
            //This  if you would like to AddAuthorization
            endpoints.MapGet("/generateJwtToken", context =>
            {
                return context.Response.WriteAsync(GenerateJwtToken(context.Request.Query["username"], context.Request.Query["password"]));
            });
        });
    }
    //Demo of a simple jwtToken implementation
    private String GenerateJwtToken(string username, string password)
    {
        string clientId_From_Db;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Name or password is not specified.");
        }

        if ((username != "demoIos" || username != "demoDroid") && password != "1234")
        {
            throw new InvalidOperationException("Name or password is not specified.");
        }
        else
        {
            if (username != "demoIos")
                clientId_From_Db = "Ios_clientid";
            else
                clientId_From_Db = "Droid_clientid";
        }
        var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim("ClientId", clientId_From_Db) };
        var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("ExampleServer", "ExampleClients", claims, expires: DateTime.Now.AddMinutes(60), signingCredentials: credentials);
        return String.Format("{0}|{1}", JwtTokenHandler.WriteToken(token), clientId_From_Db);

    }
    
}
``` 

## Instructions setup client
Project support .Net Core 5.0 or .NetStandard 2.1  

Step 1, create Xamarin Forms App and Install NuGet Package:  
Select the Xamarin Forms Template and then target .NetStandard 2.1 in the shared project.  
WallTec.CoreCom.Client in the .NetSandrad or 5.0 project.    

Step 2: Declare a client, connection options and setup-function.  
For more information about the diffrent settings read documentation below.   
```csharp
public  CoreComClient CoreComClient = new CoreComClient();
private CoreComOptions _coreComOptions;

public bool SetupCoreComServer()
{
    _coreComOptions = new CoreComOptions
    {   //debug on android emulator
        ServerAddress = (Device.RuntimePlatform == Device.Android ? "https://10.0.2.2:5001" : "https://localhost:5001"),
        DatabaseMode = DatabaseModeEnum.UseImMemory,
        GrpcOptions = new GrpcOptions
        {
            RequestServerQueueIntervalSec = 30,
            MessageDeadlineSec = 30
        },
        LogSettings = new LogSettings
        {
            LogErrorTarget = LogErrorTargetEnum.NoLoging,
            LogEventTarget = LogEventTargetEnum.NoLoging,
            LogMessageTarget = LogMessageTargetEnum.NoLoging
        }
    };
//Debug local on mac where the server is running in "Kestrel": { "EndpointDefaults": { "Protocols": "Http1"  }  }
#if DEBUG
      _coreComOptions.DangerousAcceptAnyServerCertificateValidator = true;
#endif
  return true;
}
//This is needed if you would like to use jwt tooken for validate users
public async Task<bool> Authenticate(string username, string password)
{
    try
    {
       App.ConsoleWriteLineDebug($"Authenticating as {username}...");
        var httpClientHandler = new HttpClientHandler();
        //this is so you can debug on mac and emulator the server has "EndpointDefaults": { "Protocols": "Http1"
        // Return `true` to allow certificates that are untrusted/invalid
        if (_coreComOptions.DangerousAcceptAnyServerCertificateValidator)
            httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        var httpClient = new HttpClient(httpClientHandler);

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{_coreComOptions.ServerAddress}/generateJwtToken?username={HttpUtility.UrlEncode(username)}&password=    {HttpUtility.UrlEncode(password)}"),
            Method = HttpMethod.Get,
            Version = new Version(2, 0),

        };
        var tokenResponse = await httpClient.SendAsync(request);
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadAsStringAsync();
        App.ConsoleWriteLineDebug("Successfully authenticated.");
        string[] values = token.Split("|");

        _coreComOptions.ClientToken = values[0];
        _coreComOptions.ClientId = values[1];

        return true;
    }
    catch (Exception ex)
    {
        await App.Current.MainPage.DisplayAlert("CoreCom", ex.Message + " Press Reauthorize try again", "Ok");
        return false;
    }
}

public async Task<bool> ConnectCoreComServer()
{
    //This is anly needed if you want to use the validate/login users on the server side
    #region "Authentication with backen token and clientId from database"
    //coreComOptions.ClientId and coreComOptions.ClientToken is set inside the Authenticate method
    string username = (Device.RuntimePlatform == Device.Android ? "demoDroid" : "demoIos"); //simulate diffrent user
    var token = await Authenticate(username, "1234").ConfigureAwait(false);
    if (!token)
        return false;
    #endregion

    #region "No Authentication"
    //Cross-Platform Identifier for the app stay the same as long the app is installed
    //in this senario all backend API is public and the server use guid below to seperate diffrent users requests
    //var id = Preferences.Get("my_id", string.Empty);
    //if (string.IsNullOrWhiteSpace(id))
    //{
    //    id = System.Guid.NewGuid().ToString();
    //    Preferences.Set("my_id", id);
    //}
    //coreComOptions.ClientId = id;
    #endregion

    CoreComClient.Connect(_coreComOptions);

    return true;
}

// Setup the server for example in a service class or from your App.xaml.cs 
protected async override void OnStart()
{
    SetupCoreComServer();
    await ConnectCoreComServer();
}
```       
Step 3: Register function for incoming messages.   
First create constants for the diffrent messages you would likte to send.  
after that Register the function that you whant the framework to trigger on recive messages and implement the functions.    

```csharp
public static string AddProject = "AddProject";
public static string DeleteProject = "DeleteProject";
public static string RequestAllProjects = "RequestAllProjects";
public static string ResponseAllProjects = "ResponseAllProjects";

CoreComClient.Register<List<Project>>(CoreComSignatures.ResponseAllProjects, GetAllProjects);
CoreComClient.Register<Result<Project>>(CoreComSignatures.AddProject, GetAddedProject);

private async void GetAllProjects(List<Project> projects)
{
  Projects = new ObservableCollection<Project>(projects);
}

private async void GetAddedProject(Result<Project> result)
{
    //the server wrapps in a result object to be able to return errors/validation 
    if (!result.WasSuccessful)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Code to run on the main thread
            await DialogService.ShowAlertAsync(result.UserMessage, "CoreCom", "Ok");
        });

        return;
    }
    Projects.Add(result.Model);

}
``` 
Step 4: Send messges to server.  
Below you se two different send implementations one with authentication and one not. For more information regarding authentication se the sample code. 
```csharp
 await CoreComClient.SendAuthAsync(new Project 
        { Name = "Mac Os Test", Description = "project added from client",Base64Image = _base64 }, CoreComSignatures.AddProject);  
        
 await CoreComClient.SendAsync(CoreComSignatures.RequestAllProjects);
     
``` 

There are som more features that can be good to know about that you can implement.
Some event listners for connection changes and more. 

```csharp
CoreComClient.OnConnectionStatusChange += _coreComClient_OnConnectionStatusChange;
CoreComClient.OnLatestRpcExceptionChange += _coreComClient_OnLatestRpcExceptionChange;
CoreComClient.OnLogEventOccurred += _coreComClient_OnLogEventOccurred;
``` 

# Client
Client side use gRPC-Web as framework to handle communication between clients and server. The client can run in different modes depending on the need for logging and offline support. 

## DatabaseMode

### Queue in memory mode
The server use a Entity Framework Core in momory database. All current messages is stored in the memory and when a client has sent its message to the server it's removed from the memory. If you restart the app alla outgoing queues is removed.

### Queue in databas mode 
The server use a Entity Framework Core connected database to store/handle messages queue. In this mode the server keep a database row for all messages that goes in and out from the server. We only store messages that are in progress when the are deliverd the are removed. To keep all transactions in the database select the logging setting "Logging to database". The database that are use in this senario is SQLite database. 

## LogSettings
Loggsettings is the rull of how and if you would like to logincoming and outgoing messages, Events and Erros. There is also possible to listen to events for this. 

### LogMessageTarget  

#### Database
The server log all messages to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. The tables are named OutgoingMessages and IncommingMessages. If you use this setting the framework will use a sqllite database.   

#### Message logging to file
The server log all messages to the files IncommingMessages.log and utgoningMessages.log that are stored in the app folder. In this case the messageobject is parsed as json in the logfile.

#### No message Logging 
We do no logging of messages.

### LogEventTarget
Event logging is the rull of how and if you would like to save loggs of all transactions of messages and connection changes. The table EventLogs store this information if you target database and if you target file it will be named EventLogs.log 

#### Event logging to database
The server log all transaction to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. Just the typ of message and size will be logged not the containing object.  

#### Event logging to file
The server log all messages to the files EventLogs.log that are stored in the app folder. 

#### No Event logging 
We do no logging of events.

### LogErrorTarget
Error logging is the rull of how and if you would like to save loggs of all handled errors in the framework. The table ErrorLogs store this information if you target database and if you target file it will be named ErrorLogs.log 

#### Error logging to database
The server log all errors to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. Just the typ of message and size will be logged not the containing object.  

#### Error logging to file
The server log all errors to the files errosLogs.log that are stored in the app folder. 

#### No Error logging 
We do no logging of erros.

### LogXxxxHistoryDays
LogMessageHistoryDays, LogEventHistoryDays,LogErrorHistoryDays property is used to clean database table from old rows. the default property for this is 7 days.  

# Server
Server side use gRPC-Web as framework to handle communication between server and clients. The server can run in different modes depending on the need for logging and offline support. 

Scenarios of user verifications can be built outside the framework and thus were independent of the framework. After verifying users, the framework is provided with a client ID which is then used to verify the user between client and server.

### JSON Web Token support 
You can use standard ASP.net web token to validate that the client is Authorize to send messages to the server. In this senario you able to have both public API and API that require Authorize by valid token. You only set this token once then the framwark handle when to add it or not to the request to server. 


## DatabaseMode 
The server support in Memory database, Sqlite and SQL server. 
To browse the database if you chose sqlite use sqlite viewer https://sqlitebrowser.org/dl/ The db file should be in your AppService folder 

### UseImMemory
The server use a Entity Framework Core in memory database. All current clients and messages is stored in the memory and when a client has received its message the message is removed from the memory. If you restart the server alla outgoing queues is removed.  

### UseSqlite and UseSqlServer
The server use a Entity Framework Core connected database to store/handle messages queue. In this mode the server keep a database row for all messages that goes in and out from the server. We only store messages that are in progress when the are deliverd the are removed. to keep all transactions in the database select the logging setting "Logging to database"   

in this case you also need to set the connectionstring options in appsettings.json file.

## LogMessageTarget  

#### Database
The server log all messages to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. The tables are named OutgoingMessages and IncommingMessages. If you use this setting the framework will use a sqllite database.   

#### TextFile
The server log all messages to the files IncommingMessages.log and utgoningMessages.log that are stored in the app folder. In this case the messageobject is parsed as json in the logfile.

#### NoLoging 
We do no logging of messages.

### LogEventTarget
Event logging is the rull of how and if you would like to save loggs of all transactions of messages and connection changes. The table EventLogs store this information if you target database and if you target file it will be named EventLogs.log 

#### Event logging to database
The server log all transaction to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. Just the typ of message and size will be logged not the containing object.  

#### Event logging to file
The server log all messages to the files EventLogs.log that are stored in the app folder. 

#### No Event logging 
We do no logging of events.

### LogErrorTarget
Error logging is the rull of how and if you would like to save loggs of all handled errors in the framework. The table ErrorLogs store this information if you target database and if you target file it will be named ErrorLogs.log 

#### Error logging to database
The server log all errors to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. Just the typ of message and size will be logged not the containing object.  

#### Error logging to file
The server log all errors to the files errosLogs.log that are stored in the app folder. 

#### No Error logging 
We do no logging of erros.

### LogXxxxHistoryDays
LogMessageHistoryDays, LogEventHistoryDays,LogErrorHistoryDays property is used to clean database table from old rows. the default property for this is 7 days.  


# Sample code
In this repository you have one server sample and one client sample. 
The server use Asp.Net Core 3.1 and validate the users by JWToken. 
The client use Xamarin forms and .net standard 2.1.

### Importent
The biggest problems with these types of projects for those who do not work with web and cert etc are usually different connection problems between test server with untrusted cert and debugging from clients runing on emulator.

The Example projects have some settings to solve this hopefully. 
In the server file appsettings.json(Release), the protocols are set to Http2 , this works perfectly if you publish it on azure where you then get an approved certificate who is trusted. If you want to debug localy (on mac https://go.microsoft.com/fwlink/?linkid=2099682 ) you need to set this value to Http1 as it is in the developer version of appsettings.Development.json(Debug).

This make the server run on a mac and you will be able to access it throw https://localhost:5001  
Now when we have a utrusted certificate on the server side we need to tell the client that
we trust all certificates. We do this by setting the flag DangerousAcceptAnyServerCertificateValidator = true on CoreComOptions that we provide to the CoreCom Client. 
In the sample code you will see that we use this flag above to say the same thing to httphandler for JWtoken as well. 





