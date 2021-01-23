using System;
namespace WallTec.CoreCom.Models
{
    public class LogSettings
    {
        public LogMessageTargetEnum LogMessageTarget { get; set; }
        /// <summary>
        /// number of days the message history is saved
        /// </summary>
        public int LogMessageHistoryDays { get; set; }
        /// <summary>
        /// number of days the logEvents history is saved
        /// </summary>
        public int LogEventHistoryDays { get; set; }
        public LogEventTargetEnum LogEventTarget { get; set; }
        /// <summary>
        /// number of days the logErros history is saved
        /// </summary>
        public int LogErrorHistoryDays { get; set; }
        public LogErrorTargetEnum LogErrorTarget { get; set; }

        public LogSettings()
        {
            LogMessageTarget = LogMessageTargetEnum.NoLoging;
            LogMessageHistoryDays = 7;
            LogEventTarget = LogEventTargetEnum.NoLoging;
            LogEventHistoryDays = 7;
            LogErrorTarget = LogErrorTargetEnum.NoLoging;
            LogErrorHistoryDays = 7;
        }

    }
}
