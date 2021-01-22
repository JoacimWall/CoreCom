using WallTec.CoreCom.Models;

namespace WallTec.CoreCom.Server.Models
{
    public class CoreComOptions
    {
        public LogSettings LogSettings { get; set; }
        public DatabaseModeEnum DatabaseMode { get; set; }
        public CoreComOptions()
        {
            LogSettings = new LogSettings();
            DatabaseMode = DatabaseModeEnum.UseImMemory;
        }
    }
    public enum DatabaseModeEnum : byte
    {
        UseImMemory = 0,
        UseSqlite = 1,
        UseSqlServer = 2

    }

}
