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
            DatabaseMode = DatabaseModeEnum.UseInMemory;
        }
    }
    public enum DatabaseModeEnum : byte
    {
        UseInMemory = 0,
        UseSqlite = 1,
        UseSqlServer = 2

    }

}
