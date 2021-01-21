For Android to work change http2 to 1.
 "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1"
    }

For iOS to bind properly, a new xml tag will need to be added to the Grpc.Core.targets file.
The file can be found at myProject/packages/Grpc.Core.{Version}/build/Xamarin.iOS10/Grpc.Core.targets.
The following tag should be added to both libraries references in that file:

<IsCxx>True</IsCxx>