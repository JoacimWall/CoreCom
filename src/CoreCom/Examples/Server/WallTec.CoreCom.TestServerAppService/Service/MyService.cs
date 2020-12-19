using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Server;
using WallTec.CoreCom.Sheard;

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
            _coreComService.Register(GetAllProjectsFromDb, CoreComSignatures.RequestAllProjects);
            //This need that the user got token
            _coreComService.RegisterAuth(AddProjectsToDb, CoreComSignatures.AddProject, new Project().GetType());

        }

        private async Task<bool> AddProjectsToDb(object value, CoreComUserInfo arg)
        {
            _fakeDb.AddProject(value as Project);
            //send the new projet to all client that are connected 
            foreach (var item in _coreComService.Clients)
            {
                await _coreComService.SendAsync(value as Project, CoreComSignatures.AddProject, new CoreComUserInfo { ClientId = item.CoreComUserInfo.ClientId });
            }
            return true;
        }

       
        private async Task GetAllProjectsFromDb(CoreComUserInfo coreComUserInfo)
        {
            await _coreComService.SendAsync(_fakeDb.GetAllProjects(), CoreComSignatures.ResponseAllProjects, coreComUserInfo);
        }

    
    }
}
