using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenTBNConfig : IEntityTypeConfiguration<MolenTBN>
    {
        public void Configure(EntityTypeBuilder<MolenTBN> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(ai => ai.MolenDataId)
                .IsRequired();
            builder.HasOne(m => m.MolenData)
                .WithOne(md => md.MolenTBN)
                .HasForeignKey<MolenTBN>(m => m.MolenDataId);
        }
    }
}
