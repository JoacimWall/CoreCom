using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WallTec.CoreCom.Client.Models;
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Sheard;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting!");
            WallTec.CoreCom.Client.CoreComClient coreComClient = new WallTec.CoreCom.Client.CoreComClient();
            //WPF Server
            //var response = coreComClient.CreateClient("192.168.2.121", 50060).Result;
            //other
            var response = coreComClient.Connect(new CoreComOptions { ServerAddress="https://localhost:5001"});
            if (response)
            {
              

                Console.WriteLine("Connection response: " + response.ToString());
                coreComClient.Register(GetAllProjectsFromDb, CoreComSignatures.ResponseAllProjects, new Project().GetType());
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                coreComClient.SendAsync(CoreComSignatures.RequestAllProjects);
               
               // coreComClient.ShutdownAsync().Wait();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

            }
          

        }
        private static async Task GetAllProjectsFromDb(object value, CoreComUserInfo coreComUserInfo)
        {
            var project = value as List<Project>;
            foreach (var item in project)
            {
                Console.WriteLine(item.Name);
            }
        }
    }
}
