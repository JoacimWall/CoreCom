using Microsoft.EntityFrameworkCore;
using WallTec.CoreCom.Proto;
using WallTec.CoreCom.Sheard.Models;

namespace WallTec.CoreCom.Server
{
    public class CoreComContext : DbContext
    {
        public CoreComContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }
    
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransferStatus>().Property(p => p.TransferStatusId).ValueGeneratedNever();

            //Seed data
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 0, Name = "New"});
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 1, Name = "Transferred" });
            modelBuilder.Entity<TransferStatus>().HasData(new TransferStatus { TransferStatusId = 2, Name = "Recived" });
        }
        internal DbSet<LogEvent> LogEvents { get; set; }
        internal DbSet<LogError> LogErros { get; set; }
        internal DbSet<TransferStatus> TransferStatus { get; set; }
        internal DbSet<CoreComMessage> IncomingMessages { get; set; }
        internal DbSet<CoreComMessageResponse> OutgoingMessages { get; set; }
    }
}
