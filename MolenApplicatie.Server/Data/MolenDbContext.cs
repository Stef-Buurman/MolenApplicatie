using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Models.MariaDB;

namespace MolenApplicatie.Server.Data
{
    public class MolenDbContext : DbContext
    {
        public MolenDbContext(DbContextOptions<MolenDbContext> options) : base(options) { }

        public DbSet<MolenTBN> MolenTBNs { get; set; }
        public DbSet<MolenData> MolenData { get; set; }
        public DbSet<MolenType> MolenTypes { get; set; }
        public DbSet<MolenTypeAssociation> MolenTypeAssociations { get; set; }
        public DbSet<LastSearchedForNewData> LastSearchedForNewDatas { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<DisappearedYearInfo> DisappearedYearInfos { get; set; }
        public DbSet<MolenMaker> MolenMakers { get; set; }
        public DbSet<MolenImage> MolenImages { get; set; }
        public DbSet<AddedImage> AddedImages { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MolenDbContext).Assembly);
        }
    }
}
