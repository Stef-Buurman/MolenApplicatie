using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenMakerConfig : IEntityTypeConfiguration<MolenMaker>
    {
        public void Configure(EntityTypeBuilder<MolenMaker> builder)
        {
            builder.HasOne(m => m.MolenData)
                .WithMany(md => md.MolenMakers)
                .HasForeignKey(md => md.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
