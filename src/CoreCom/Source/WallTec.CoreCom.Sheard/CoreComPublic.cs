using System;
namespace WallTec.CoreCom.Sheard
{
    public class CoreComUserInfo
    {
        public string ClientId { get; set; }
    }

    public class LogEvent
    {
        public string MessagesSignature { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum TransferStatus { get; set; }
    }
    public enum TransferStatusEnum
    {
        New = 0,
        InProcess = 1,
        Recived = 2,
        Transferred = 3,
        Done = 4
    }
}
