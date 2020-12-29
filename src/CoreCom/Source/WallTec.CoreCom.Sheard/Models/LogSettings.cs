using System;
namespace WallTec.CoreCom.Sheard.Models
{
    public class LogSettings
    {
        public LogMessageTargetEnum LogMessageTarget { get; set; }
        public LogEventTargetEnum LogEventTarget { get; set; }
        
        public LogSettings()
        {

            LogMessageTarget = LogMessageTargetEnum.NoLoging;
            LogEventTarget = LogEventTargetEnum.NoLoging;
           
        }

    }
}
