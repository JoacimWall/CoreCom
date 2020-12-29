# CoreCom
Framework for wrapping gRPC to be used in project that targets .Net standard (Xamarin) on the client side and .Net Core on server side.
To use this please use the nuget's WallTec.CoreCom.Client and WallTec.CoreCom.Server throw nuget manager in Visual studio. The solution build on top of grpc-web and are able to be hosted in azure or any other hosting that support ASP.NET Core. 

The framework wrapping the Proto files and gRPC logic so the client only register functions that listen to diffrent messages on the server and client side. Please view the sample more information.       

### Offline suport
The framework takes care of any connection or transmission errors and queues them to make a new transmission when connection is restored.
### Detailed logging
Detailed logging can be turned on. All transactions are written to the database or text file.

# Client
Client side use gRPC-Web as framework to handle communication between clients and server. The client can run in different modes depending on the need for logging and offline support. 

### Queue in memory mode
The server use a Entity Framework Core in momory database. All current messages is stored in the memory and when a client has sent its message to the server it's removed from the memory. If you restart the app alla outgoing queues is removed.

### Queue in databas mode (in development)
The server use a Entity Framework Core connected database to store/handle messages queue. In this mode the server keep a database row for all messages that goes in and out from the server. We only store messages that are in progress when the are deliverd the are removed. To keep all transactions in the database select the logging setting "Logging to database". The database that are use in this senario is SQLite database. 

## Message logging
Message logging is the rull of how and if you would like to save alla incoming and outgoing messages.

### Message logging to database
The server log all messages to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. The tables are named OutgoingMessages and IncommingMessages.  

### Message logging to file
The server log all messages to the files IncommingMessages.log and utgoningMessages.log that are stored in the app folder. in this case the messageobject is parsed as json in the logfile.

### No message Logging 
We do no logging of messages.

## Event logging
Event logging is the rull of how and if you would like to save loggs of all transactions of messages and connection changes. The table EventLogs store this information if you target database and if you target file it will be named EventLogs.log 

### Event logging to database
The server log all transaction to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode. Just the typ of message and size will be logged not the containing object.  

### Event logging to file
The server log all messages to the files EventLogs.log that are stored in the app folder. 

### No Event logging 
We do no logging of events.

## automatically increase deadline time on timeout messages (in development)

# Server
Server side use gRPC-Web as framework to handle communication between server and clients. The server can run in different modes depending on the need for logging and offline support. 

sqlite viewer https://sqlitebrowser.org/dl/


Scenarios of user verifications can be built outside the framework and thus were independent of the framework. After verifying users, the framework is provided with a client ID which is then used to verify the user between client and server.

## JSON Web Token support 
You can use standard ASP.net web token to validate that the client is Authorize to send messages to the server. In this senario you able to have both public API and API that require Authorize by valid token. You only set this token once then the framwark handle when to add it or not to the request to server. 

## Different server modes 

### Queue in memory mode
The server use a Entity Framework Core in momory database. All current clients and messages is stored in the memory and when a client has received its message the message is removed from the memory. If you restart the server alla outgoing queues is removed.
### Queue in databas mode
The server use a Entity Framework Core connected database to store/handle messages queue. In this mode the server keep a database row for all messages that goes in and out from the server. We only store messages that are in progress when the are deliverd the are removed. to keep all transactions in the database select the logging setting "Logging to database" 

### Logging to database
The server log all messages to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode.

### Logging all transations to file
The server log all messages to IncommingMessages.log and utgoningMessages.log

### No Logging 
We do no logging.


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





