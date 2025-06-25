using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenImageConfig : IEntityTypeConfiguration<MolenImage>
    {
        public void Configure(EntityTypeBuilder<MolenImage> builder)
        {
            builder.HasKey(mi => mi.Id);
            builder.Property(mi => mi.MolenDataId)
                .IsRequired();
            builder.HasOne(m => m.MolenData)
                .WithMany(md => md.Images)
                .HasForeignKey(m => m.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}