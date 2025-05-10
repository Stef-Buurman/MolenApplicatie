using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenTypeAssociationConfig : IEntityTypeConfiguration<MolenTypeAssociation>
    {
        public void Configure(EntityTypeBuilder<MolenTypeAssociation> builder)
        {
            builder.HasOne(mta => mta.MolenData)
               .WithMany(md => md.MolenTypeAssociations)
               .HasForeignKey(mta => mta.MolenDataId)
               .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(mta => mta.MolenType)
               .WithMany(mt => mt.MolenTypeAssociations)
               .HasForeignKey(mta => mta.MolenTypeId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
