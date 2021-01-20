using System;
namespace WallTec.CoreCom.Client.Models
{
    public class GrpcOptions
    {
        public GrpcOptions()
        {
            ConnectToServerDeadlineSec = 10;
            MessageDeadlineSec = 30;
            RequestServerQueueIntervalSec = 60;
            MessageDeadlineSecMultiplay = 1;
        }
        public int RequestServerQueueIntervalSec { get; set; }
        public int MessageDeadlineSec { get; set; }
        internal int ConnectToServerDeadlineSec { get; set; }
        internal int MessageDeadlineSecMultiplay { get; set; }
       
    }
}
