using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Sheard;


namespace WallTec.CoreCom.Server.Models
{
    public class Client
    {
  
        public CoreComUserInfo CoreComUserInfo  { get; set; }

        public bool ClientIsSending { get; set; }
      

    }
}
