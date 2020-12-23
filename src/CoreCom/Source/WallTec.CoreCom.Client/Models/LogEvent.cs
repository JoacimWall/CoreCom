using System;
using System.Transactions;
using WallTec.CoreCom.Sheard;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Client.Models
{
    public class LogEvent
    {
        public LogEvent(CoreCom.Proto.CoreComMessage coreComMessage)
        {
            MessagesSignature = coreComMessage.MessageSignature;
            TimeStampUtc = DateTime.UtcNow;
            TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus;
        }
        public LogEvent(CoreCom.Proto.CoreComMessageResponse coreComMessage)
        {
            MessagesSignature = coreComMessage.MessageSignature;
            TimeStampUtc = DateTime.UtcNow;
            TransferStatus = (TransferStatusEnum)coreComMessage.TransferStatus;
        }
        public string MessagesSignature { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum TransferStatus { get; set; }
    }
}
