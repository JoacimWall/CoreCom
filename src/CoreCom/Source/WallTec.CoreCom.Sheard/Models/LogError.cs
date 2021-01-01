using System;
namespace WallTec.CoreCom.Sheard.Models
{
    public class LogError
    {
        public LogError()
        {
            LogErrorId = Guid.NewGuid().ToString();
            TimeStampUtc = DateTime.UtcNow;
        }
        public string LogErrorId { get; set; }
        public string ClientId { get; set; }
        public string Description { get; set; }
        public string TransactionIdentifier { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Stacktrace { get; set; }
    }
}
