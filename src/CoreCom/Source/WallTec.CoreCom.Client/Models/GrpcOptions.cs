using System;
namespace WallTec.CoreCom.Client.Models
{
    public class GrpcOptions
    {
        public GrpcOptions()
        {
            ConnectToServerDeadlineSec = 5;
            MessageDeadlineSec = 30;
            RequestServerQueueIntervalSec = 30;
        }
        public int ConnectToServerDeadlineSec { get; set; }
        public int MessageDeadlineSec { get; set; }
        public int RequestServerQueueIntervalSec { get; set; }
    }
}
