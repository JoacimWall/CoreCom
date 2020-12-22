# CoreCom
Framework for wrapping gRPC to be used in side project that targets .Net standard (Xamarin) on the client side and .Net Core on server side.
To use this please use the nuget's WallTec.CoreCom.Client and WallTec.CoreCom.Server throw nuget manager in Visual studio. 

### Offline suport
Ramveket takes care of any connection or transmission errors and queues them to make a new transmission when connection is restored.
### Detailed logging
Detailed logging can be turned on. All transactions are written to the database or text file.

# Client
Client side use gRPC-Web as framework to handle communication between clients and server. The client can run in different modes depending on the need for logging and offline support. 

### Queue in memory mode
The server use a Entity Framework Core in momory database. All current messages is stored in the memory and when a client has sent its message to the server it's removed from the memory. If you restart the app alla outgoing queues is removed.

### Queue in databas mode (in development)
The server use a Entity Framework Core connected database to store/handle messages queue. In this mode the server keep a database row for all messages that goes in and out from the server. We only store messages that are in progress when the are deliverd the are removed. To keep all transactions in the database select the logging setting "Logging to database". The database that are use in this senario is SQLite database. 

### Logging to database
The server log all messages to the Entity Framework Core connected database. You should not use this if you have Queue in memory mode.

### Logging all transations to file
The server log all messages to IncommingMessages.log and utgoningMessages.log that are stored in the app folder.

### No Logging 
We do no logging.

## automatically increase deadline time on timeout messages (in development)

# Server
Server side use gRPC-Web as framework to handle communication between server and clients. The server can run in different modes depending on the need for logging and offline support. 


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




