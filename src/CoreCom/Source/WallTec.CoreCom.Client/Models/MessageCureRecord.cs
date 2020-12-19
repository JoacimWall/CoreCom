using System;
using WallTec.CoreCom.Proto;

namespace WallTec.CoreCom.Client.Models
{
    public class MessageCureRecord
    {
       
            public CoreComMessage CoreComMessage { get; set; }
            public bool SendAuth { get; set; }
    }
}
