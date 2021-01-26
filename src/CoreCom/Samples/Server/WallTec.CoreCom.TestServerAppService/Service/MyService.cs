
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Server;
using WallTec.CoreCom.Models;
using WallTec.CoreCom.Server.Models;

namespace WallTec.CoreCom.TestServerAppService.Service
{
    public class MyService : CoreComService
    {
       // private CoreComService _coreComService;
        private FakeDb _fakeDb = new FakeDb();

        public MyService(CoreComOptions coreComOptions) : base (coreComOptions)
        {
            //this functions are public
            Register(CoreComSignatures.RequestAllProjects, GetAllProjectsFromDb);
            //This need that the user have token
            RegisterAuth<Project>(CoreComSignatures.AddProject, AddProjectsToDb);
            RegisterAuth<Project>(CoreComSignatures.DeleteProject, DeleteProject);
        }
        
        private async void AddProjectsToDb(Project value,CoreComUserInfo arg)
        {
            //Validate input
            if (string.IsNullOrEmpty(value.Name))
            {
                var error = new Result<Project>("The project need a name");
                await SendAsync(error, CoreComSignatures.AddProject, arg);
                return;
            }

            _fakeDb.AddProject(value);
            //send the new projet to client 
            
            await SendAsync(new Result<Project>(value), CoreComSignatures.AddProject, new CoreComUserInfo { ClientId = arg.ClientId });
            
            
        }
        private void DeleteProject(Project value, CoreComUserInfo arg)
        {
            _fakeDb.DeleteProject(value );

        }


        private async void GetAllProjectsFromDb(CoreComUserInfo coreComUserInfo)
        {
            await SendAsync(_fakeDb.GetAllProjects(), CoreComSignatures.ResponseAllProjects, coreComUserInfo);
        }

    
    }
}
