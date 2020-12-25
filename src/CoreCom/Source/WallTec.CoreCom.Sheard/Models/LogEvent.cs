using System;

namespace WallTec.CoreCom.Sheard.Models
{
    public class LogEvent
    {
        public LogEvent()
        {
            TimeStampUtc = DateTime.UtcNow;
        }
        public string Title { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum? TransferStatus { get; set; }
        public ConnectionStatusEnum? ConnectionStatus { get; set; }
        public int MessageSize { get; set; }
    }
}
