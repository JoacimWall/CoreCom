using System;
using System.Collections.Generic;
using System.Text;
using WallTec.CoreCom.Sheard;

namespace WallTec.CoreCom.Client.Models
{
    public class CoreComOptions
    {
        public CoreComOptions()
        {
            ConnectToServerDeadlineSec = 5;
            MessageDeadlineSec = 30;
            RequestServerQueueIntervalSec = 30;
            LogMessageSource = LogMessageSourceEnum.NoLoging;
            LogEventSource = LogEventSourceEnum.NoLoging;
            DatabaseMode = DatabaseModeEnum.ImMemory;
        }
        public int ConnectToServerDeadlineSec { get; set; }
        public int MessageDeadlineSec { get; set; }
        public int RequestServerQueueIntervalSec { get; set; }
        public string ServerAddress { get; set; }
        public string ClientToken { get;  set; }
        public string ClientId { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
        public LogMessageSourceEnum LogMessageSource { get; set; }
        public LogEventSourceEnum LogEventSource { get; set; }
        public DatabaseModeEnum DatabaseMode { get; set; }
        
    }
}
