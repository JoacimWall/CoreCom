using System;
namespace WallTec.CoreCom.Sheard.Models
{
    public class LogEvent
    {
        public LogEvent()
        {
            LogEventId = Guid.NewGuid().ToString();
            TimeStampUtc = DateTime.UtcNow;
        }
        public string LogEventId { get; set; } 
        public string Description { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum? TransferStatus { get; set; }
        public ConnectionStatusEnum? ConnectionStatus { get; set; }
        public int MessageSize { get; set; }
    }
}
