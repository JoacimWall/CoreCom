using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Server.Models;

namespace WallTec.CoreCom.Server
{
    public class CoreComContext : DbContext
    {
        public CoreComContext(DbContextOptions options) : base(options)
        {
            this.Database.EnsureCreated();
        }
        //public CoreComContext(DbContextOptions<CoreComContext> options) : this((DbContextOptions)options)
        //{
        //}
        //public CoreComContext()
        //{
        //    this.Database.EnsureCreated();
        //}
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseInMemoryDatabase(databaseName: "CoreComDb");
        //    base.OnConfiguring(optionsBuilder);
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransferStatus>().Property(p => p.TransferStatusId).ValueGeneratedNever();


            //Seed data
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 0, Name = "New"});
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 1, Name = "InProcess" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 2, Name = "Recived" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 3, Name = "Transferred" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 4, Name = "Done" });
        }

        public DbSet<TransferStatus> TransferStatus { get; set; }
        public DbSet<CoreComMessage> IncomingMessages { get; set; }
        public DbSet<CoreComMessage> OutgoingMessages { get; set; }
    }
}
