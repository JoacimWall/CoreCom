using System;
using System.Collections.Generic;
using System.Text;
using WallTec.CoreCom.Sheard;

namespace WallTec.CoreCom.Server.Models
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
        public LogMessageSourceEnum LogMessageSource { get; set; }
        public LogEventSourceEnum LogEventSource { get; set; }
        public DatabaseModeEnum DatabaseMode { get; set; }

        public LogSettings()
        {

            LogMessageSource = LogMessageSourceEnum.NoLoging;
            LogEventSource = LogEventSourceEnum.NoLoging;
            DatabaseMode = DatabaseModeEnum.ImMemory;
        }

    }
   
}
