using System;
using WallTec.CoreCom.Proto;

namespace WallTec.CoreCom.Client.Models
{
    public class LogEvent
    {
        public LogEvent(CoreComMessage coreComMessage)
        {
            MessagesSignature = coreComMessage.MessageSignature;
            TimeStampUtc = DateTime.UtcNow;
            TransferStatus = coreComMessage.TransferStatus;
        }
        public LogEvent(CoreComMessageResponse coreComMessage)
        {
            MessagesSignature = coreComMessage.MessageSignature;
            TimeStampUtc = DateTime.UtcNow;
            TransferStatus = coreComMessage.TransferStatus;
        }
        public string MessagesSignature { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum TransferStatus { get; set; }
    }
}
