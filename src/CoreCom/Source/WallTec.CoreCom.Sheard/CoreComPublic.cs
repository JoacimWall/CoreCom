using System;
namespace WallTec.CoreCom.Sheard
{
    
    public enum TransferStatusEnum : byte
    {
        New = 0,
        InProcess = 1,
        Recived = 2,
        Transferred = 3,
        Done = 4
    }
    public enum ConnectionStatusEnum : byte
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2

    }
}
