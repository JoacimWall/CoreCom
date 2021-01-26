using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WallTec.CoreCom.Models;

namespace WallTec.CoreCom.Server.Models
{
    public class CoreComOptions
    {
        public LogSettings LogSettings { get; set; }
        public DatabaseModeEnum DatabaseMode { get; set; }
        internal DbContextOptions DbContextOptions { get; set; }
        internal List<Client> Clients = new List<Client>();
        public CoreComOptions(IConfiguration _config)
        {
            LogSettings = new LogSettings();
            DatabaseMode = DatabaseModeEnum.UseInMemory;

            try
            {
                LogSettings.LogMessageTarget = (LogMessageTargetEnum)System.Enum.Parse(typeof(LogMessageTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogMessageTarget"]);
                LogSettings.LogEventTarget = (LogEventTargetEnum)System.Enum.Parse(typeof(LogEventTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogEventTarget"]);
                LogSettings.LogErrorTarget = (LogErrorTargetEnum)System.Enum.Parse(typeof(LogErrorTargetEnum), _config["CoreCom:CoreComOptions:LogSettings:LogErrorTarget"]);

                LogSettings.LogMessageHistoryDays = Convert.ToInt32(_config["CoreCom:CoreComOptions:LogSettings:LogMessageHistoryDays"]);
                LogSettings.LogEventHistoryDays = Convert.ToInt32(_config["CoreCom:CoreComOptions:LogSettings:LogEventHistoryDays"]);
                LogSettings.LogErrorHistoryDays = Convert.ToInt32(_config["CoreCom:CoreComOptions:LogSettings:LogErrorHistoryDays"]);

                DatabaseMode = (DatabaseModeEnum)System.Enum.Parse(typeof(DatabaseModeEnum), _config["CoreCom:CoreComOptions:Database:DatabaseMode"]);

                string connectionstring = _config["CoreCom:CoreComOptions:Database:ConnectionString"];
                var optionsBuilder = new DbContextOptionsBuilder<CoreComContext>();

                switch (DatabaseMode)
                {
                    case DatabaseModeEnum.UseInMemory:
                        optionsBuilder.UseInMemoryDatabase(databaseName: "CoreComDb");
                        break;
                    case DatabaseModeEnum.UseSqlite:
                        optionsBuilder.UseSqlite(connectionstring);
                        optionsBuilder.UseBatchEF_Sqlite();
                        break;
                    case DatabaseModeEnum.UseSqlServer:
                        optionsBuilder.UseSqlServer(connectionstring);
                        optionsBuilder.UseBatchEF_MSSQL();
                        break;
                    default:
                        break;
                }

                DbContextOptions = optionsBuilder.Options;
            }
            catch (Exception ex)
            {
                //LogErrorOccurred(ex, null);

            }

        }
    }
    public enum DatabaseModeEnum : byte
    {
        UseInMemory = 0,
        UseSqlite = 1,
        UseSqlServer = 2

    }

}
