using System;
using System.Collections.Generic;
using System.Text;

namespace WallTec.CoreCom.Client.Models
{
    public class CoreComOptions
    {
        public CoreComOptions()
        {
            ConnectToServerDeadLineSec = 5;
            MessageDeadLineSec = 30;
        }
        public int ConnectToServerDeadLineSec { get; set; }
        public int MessageDeadLineSec { get; set; }
        public string ServerAddress { get; set; }
        public string ClientToken { get;  set; }
    }
}
