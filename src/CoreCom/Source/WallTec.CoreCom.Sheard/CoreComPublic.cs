using System;
namespace WallTec.CoreCom.Sheard
{
    
    public enum TransferStatusEnum : byte
    {
        New = 0,
        Recived = 1,
        Transferred = 2,

    }
    public enum ConnectionStatusEnum : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2

    }
    public enum DatabaseModeEnum : byte
    {
        ImMemory = 0,
        OnDisk = 1

    }
    public enum LogMessageSourceEnum : byte
    {
        NoLoging = 0,
        TextFile = 1,
        Database = 2


    }
    public enum LogEventSourceEnum : byte
    {
        NoLoging = 0,
        TextFile = 1,
        Database = 2


    }
}
