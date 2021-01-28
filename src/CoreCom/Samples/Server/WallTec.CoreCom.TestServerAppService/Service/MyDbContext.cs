using System;
using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Example.Shared.Entitys;

namespace WallTec.CoreCom.TestServerAppService.Service
{
 
   public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ////Seed data
            modelBuilder.Entity<Project>().HasData(new Project { ProjectId = new Guid("{578C2C66-BC21-4BB4-B49A-22D70F988753}"), Name = "Windows 10Z", Description = "Linux port of Windows" });
            modelBuilder.Entity<Project>().HasData(new Project { ProjectId = new Guid("{99A91FCF-2982-40C4-AAC4-9538404C5AA5}"), Name = "Mac OS X amd", Description = "Arm port of Mac OS" });

        }
        public DbSet<Project> Projects { get; set; }

    }
}
