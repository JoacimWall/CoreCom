using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Sheard.Models;
namespace WallTec.CoreCom.Client
{
    internal class CoreComContext : DbContext
    {
        public CoreComContext(DbContextOptions options) : base(options)
        {
            SQLitePCL.Batteries_V2.Init();

            
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
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 0, Name = "New" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 1, Name = "Recived" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 2, Name = "Transferred" });
        }
        internal DbSet<LogEvent> LogEvents { get; set; }
        internal DbSet<LogError> LogErros { get; set; }
        internal DbSet<TransferStatus> TransferStatus { get; set; }
        internal DbSet<CoreComMessageResponse> IncomingMessages { get; set; }
        internal DbSet<CoreComMessage> OutgoingMessages { get; set; }
    }
}
