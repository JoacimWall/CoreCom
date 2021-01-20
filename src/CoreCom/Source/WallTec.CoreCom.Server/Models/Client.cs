using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Server.Models
{
    public class Client
    {
        public CoreComUserInfo CoreComUserInfo  { get; set; }
        internal bool ClientIsSending { get; set; }
    }
}
