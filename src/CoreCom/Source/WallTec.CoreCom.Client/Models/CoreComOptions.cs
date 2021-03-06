﻿
using WallTec.CoreCom.Models;

namespace WallTec.CoreCom.Client.Models
{
    public class CoreComOptions
    {
        public CoreComOptions()
        {
            
            LogSettings = new LogSettings();
            GrpcOptions = new GrpcOptions();
    }
        public LogSettings LogSettings { get; set; }
        public GrpcOptions GrpcOptions { get; set; }
        public DatabaseModeEnum DatabaseMode { get; set; }

        
        public string ServerAddress { get; set; }
        public string ClientToken { get;  set; }
        public string ClientId { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
       
        
    }
    public enum DatabaseModeEnum : byte
    {
        UseInMemory = 0,
        UseSqlite = 1
       

    }
}
