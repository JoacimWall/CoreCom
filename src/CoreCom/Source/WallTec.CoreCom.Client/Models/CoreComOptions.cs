using System;
using System.Collections.Generic;
using System.Text;

namespace WallTec.CoreCom.Client.Models
{
    public class CoreComOptions
    {
        public CoreComOptions()
        {
            ConnectToServerDeadlineSec = 5;
            MessageDeadlineSec = 30;
            RequestServerQueueIntervalSec = 30;
        }
        public int ConnectToServerDeadlineSec { get; set; }
        public int MessageDeadlineSec { get; set; }
        public int RequestServerQueueIntervalSec { get; set; }
        public string ServerAddress { get; set; }
        public string ClientToken { get;  set; }
        public string ClientId { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
    }
}
