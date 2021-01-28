
using WallTec.CoreCom.Example.Shared.Entitys;
using WallTec.CoreCom.Example.Shared;
using WallTec.CoreCom.Server;
using WallTec.CoreCom.Models;
using WallTec.CoreCom.Server.Models;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace WallTec.CoreCom.TestServerAppService.Service
{
    public class MyService : CoreComService
    {
        
        private IServiceScopeFactory _serviceScopeFactory;
        public MyService(IServiceScopeFactory serviceScopeFactory, CoreComOptions coreComOptions) : base (coreComOptions)
        {
            _serviceScopeFactory = serviceScopeFactory;
            //this functions are public
            Register(CoreComSignatures.RequestAllProjects, GetAllProjectsFromDb);
           
            //This need that the user have token
            RegisterAuth<Project>(CoreComSignatures.AddProject, AddProjectsToDb);
            RegisterAuth<Project>(CoreComSignatures.DeleteProject, DeleteProject);
        }
        private void DeleteProject(Project value, CoreComUserInfo arg)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<MyDbContext>();

                var remove = dbContext.Projects.FirstOrDefault(x => x.ProjectId == value.ProjectId);
                if (remove != null)
                {
                    dbContext.Remove(remove);
                    dbContext.SaveChangesAsync();
                }
            }

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
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<MyDbContext>();

                value.ProjectId = System.Guid.NewGuid();
                dbContext.Projects.Add(value);

                dbContext.SaveChanges();
                //send the new projet to client 
                await SendAsync(new Result<Project>(value), CoreComSignatures.AddProject, new CoreComUserInfo { ClientId = arg.ClientId });

            }

        }

        private async void GetAllProjectsFromDb(CoreComUserInfo coreComUserInfo)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<MyDbContext>();

                var list = dbContext.Projects.ToList();
                await SendAsync(list, CoreComSignatures.ResponseAllProjects, coreComUserInfo);
            }

        }
        

    }
}
