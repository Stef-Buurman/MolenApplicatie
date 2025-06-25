using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenTypeConfig : IEntityTypeConfiguration<MolenType>
    {
        public void Configure(EntityTypeBuilder<MolenType> builder)
        {
            builder.HasKey(mt => mt.Id);
            builder.HasMany(mt => mt.MolenTypeAssociations)
                .WithOne(mta => mta.MolenType)
                .HasForeignKey(mta => mta.MolenTypeId);
            builder.HasIndex(p => p.Name)
                .IsUnique();
        }
    }
}
