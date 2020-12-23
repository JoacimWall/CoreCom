using System;
namespace WallTec.CoreCom.Sheard.Models
{
    public class LogEvent
    {
        public LogEvent()
        {
            TimeStampUtc = DateTime.UtcNow;
        }
        public string MessagesSignature { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum TransferStatus { get; set; }
    }
}
