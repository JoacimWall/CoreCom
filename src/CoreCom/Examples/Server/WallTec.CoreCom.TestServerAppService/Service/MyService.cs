using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Server;
using WallTec.CoreCom.Sheard;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.TestServerAppService.Service
{
    public class MyService : IMyService
    {
        private CoreComService _coreComService;
        private FakeDb _fakeDb = new FakeDb();

        public MyService(CoreComService coreComService)
        {
            _coreComService = coreComService;
            //This public
            _coreComService.Register(CoreComSignatures.RequestAllProjects, GetAllProjectsFromDb);
            //This need that the user got token
            // _coreComService.RegisterAuth(AddProjectsToDb, CoreComSignatures.AddProject, new Project().GetType());
            _coreComService.Register<Project>(CoreComSignatures.AddProject, AddProjectsToDb,true);
        }
        
        private async void AddProjectsToDb(Project value, CoreComUserInfo arg)
        {
            //Validate input
            if (string.IsNullOrEmpty(value.Name))
            {
                var error = new Result<Project>("The project need a name");
                await _coreComService.SendAsync(error, CoreComSignatures.AddProject, arg);
                return;
            }

            _fakeDb.AddProject(value as Project);
            //send the new projet to all client that are connected 
            foreach (var item in _coreComService.Clients)
            {
                await _coreComService.SendAsync(value as Project, CoreComSignatures.AddProject, new CoreComUserInfo { ClientId = item.CoreComUserInfo.ClientId });
            }
            
        }

       
        private async void GetAllProjectsFromDb(CoreComUserInfo coreComUserInfo)
        {
            await _coreComService.SendAsync(_fakeDb.GetAllProjects(), CoreComSignatures.ResponseAllProjects, coreComUserInfo);
        }

    
    }
}
