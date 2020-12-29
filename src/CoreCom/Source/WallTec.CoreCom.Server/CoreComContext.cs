using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Sheard.Models;

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
        //    //"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-MvcMovie-4ae3798a;Trusted_Connection=True;MultipleActiveResultSets=true"
        //    //}
        //    optionsBuilder.UseSqlite(@"Data Source=CoreComDb.db");
        //    //optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=CoreComDb;Trusted_Connection=True;MultipleActiveResultSets=true");
        //    //optionsBuilder.UseInMemoryDatabase(databaseName: "CoreComDb");
        //    base.OnConfiguring(optionsBuilder);
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransferStatus>().Property(p => p.TransferStatusId).ValueGeneratedNever();


            //Seed data
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 0, Name = "New"});
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 1, Name = "Recived" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 2, Name = "Transferred" });
        }
        internal DbSet<LogEvent> LogEvents { get; set; }
        internal DbSet<TransferStatus> TransferStatus { get; set; }
        internal DbSet<CoreComMessage> IncomingMessages { get; set; }
        internal DbSet<CoreComMessageResponse> OutgoingMessages { get; set; }


       
    }
}
