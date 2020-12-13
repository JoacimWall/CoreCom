using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WallTec.CoreCom.Client.Models;
using WallTec.CoreCom.Example.SharedObjects;
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
            var response = coreComClient.Connect(new CoreComOptions { ServerAddress="http://localhost:5001", ClientIsMobile=true});
            if (response)
            {
              

                Console.WriteLine("Connection response: " + response.ToString());
                coreComClient.Register(GetAllProjectsFromDb, CoreComSignatures.ResponseAllProjects, new List<Project>().GetType());
                Console.WriteLine("Wait 2 sec for connection to backend is upp the press key...");
                Console.ReadKey();
                coreComClient.SendAsync(CoreComSignatures.RequestAllProjects);
               
               // coreComClient.ShutdownAsync().Wait();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

            }
          

        }
        private static async Task GetAllProjectsFromDb(object value, CoreComUserInfo coreComUserInfo)
        {
            var projects = value as List<Project>;
            foreach (var item in projects)
            {
                Console.WriteLine(item.Name);
            }

        }
    }
}
