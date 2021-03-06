﻿using System;
using WallTec.CoreCom.Models;

namespace WallTec.CoreCom.Models
{
    public class LogEvent
    {
        public LogEvent()
        {
            LogEventId = Guid.NewGuid().ToString();
            TimeStampUtc = DateTime.UtcNow;
        }
        public string LogEventId { get; set; }
        public string ClientId { get; set; }
        public string Description { get; set; }
        public string TransactionIdentifier { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public TransferStatusEnum? TransferStatus { get; set; }
        public ConnectionStatusEnum? ConnectionStatus { get; set; }
        public int MessageSize { get; set; }
    }
}
