using System;
using System.Collections.Generic;
using System.Text;

namespace WallTec.CoreCom.Client.Models
{
    public class CoreComOptions
    {
        //public int PortNumber { get; set; }
        public string ServerAddress { get; set; }
        public bool ClientIsMobile { get; set; }
        public string ClientToken { get; set; }
    }
}
