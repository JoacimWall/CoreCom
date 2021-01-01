using System;
namespace WallTec.CoreCom.Sheard
{
    
    public enum TransferStatusEnum : byte
    {
        New = 0,
        Transferred = 1,
        Recived = 2,

    }
    public enum ConnectionStatusEnum : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2

    }
    public enum DatabaseModeEnum : byte
    {
        UseImMemory = 0,
        UseSqlite = 1,
        UseSqlServer = 2

    }
    public enum LogMessageTargetEnum : byte
    {
        NoLoging = 0,
        TextFile = 1,
        Database = 2
    }
    public enum LogEventTargetEnum : byte
    {
        NoLoging = 0,
        TextFile = 1,
        Database = 2
    }
    public enum LogErrorTargetEnum : byte
    {
        NoLoging = 0,
        TextFile = 1,
        Database = 2


    }
}
