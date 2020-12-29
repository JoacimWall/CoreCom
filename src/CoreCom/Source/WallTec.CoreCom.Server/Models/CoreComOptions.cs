using System;
using System.Collections.Generic;
using System.Text;
using WallTec.CoreCom.Sheard;
using WallTec.CoreCom.Sheard.Models;

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
    
   
}
