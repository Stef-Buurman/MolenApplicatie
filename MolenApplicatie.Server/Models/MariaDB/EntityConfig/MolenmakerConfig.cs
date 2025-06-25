using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenMakerConfig : IEntityTypeConfiguration<MolenMaker>
    {
        public void Configure(EntityTypeBuilder<MolenMaker> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(ai => ai.MolenDataId)
                .IsRequired();
            builder.HasOne(m => m.MolenData)
                .WithMany(md => md.MolenMakers)
                .HasForeignKey(m => m.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
