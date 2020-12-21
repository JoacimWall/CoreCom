using System;
using System.Collections.Generic;
using System.Text;

namespace WallTec.CoreCom.Server
{
    public class CoreComOptions
    {
        public LogSettings LogSettings { get; set; }

        public CoreComOptions()
        {
            LogSettings = new LogSettings();


        }
    }
    public class LogSettings
    {
        public logSource logSource { get; set; }

        public LogSettings()
        {

            logSource = logSource.NoLoging;
        }

    }
    public enum logSource
    { 
         NoLoging=0,
         TextFile=1

    
    }
}
