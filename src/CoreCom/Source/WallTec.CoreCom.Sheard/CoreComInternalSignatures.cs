using System;
using System.Collections.Generic;
using System.Text;

namespace WallTec.CoreCom.Sheard
{
    public static class CoreComInternalSignatures
    {
        public const string CoreComInternal = "CoreComInternal";
        public const string CoreComInternal_ConnectInstallId = "CoreComInternal_ConnectInstallId";
        public const string CoreComInternal_PullCue = "CoreComInternal_PullCue";
    }
    public enum TransferStatus : int
    {
        New = 0,
        InProcess=1,
        Transferred=2    

    }
}
