using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using WallTec.CoreCom.Example.SharedObjects;

namespace WallTec.CoreCom.TestServerAppService.Service
{
    public class FakeDb
    {
        private List<Project> _projects = new List<Project>();
        public FakeDb()
        {
            AddEntitys();
        }

        private void AddEntitys()
        {
            _projects.Add(new Project { Name = "Windows 10Z", Description = "Linux port of Windows" });
            _projects.Add(new Project { Name = "Mac OS X amd", Description = "amd port of Mac OS" });
        }


        #region Public functions
        public List<Project> GetAllProjects()
        {
            return _projects;

        }
        public Project AddProject(Project newProject)
        {
            _projects.Add(newProject);
            return newProject;

        }
        #endregion

    }
}
