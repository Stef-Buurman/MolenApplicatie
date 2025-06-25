using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class MolenDataConfig : IEntityTypeConfiguration<MolenData>
    {
        public void Configure(EntityTypeBuilder<MolenData> builder)
        {
            builder.HasKey(m => m.Id);
            builder.HasAlternateKey(m => m.Ten_Brugge_Nr);
            builder.HasMany(m => m.AddedImages)
                .WithOne(ai => ai.MolenData)
                .HasForeignKey(ai => ai.MolenDataId);
            builder.HasMany(m => m.Images)
                .WithOne(mi => mi.MolenData)
                .HasForeignKey(mi => mi.MolenDataId);
            builder.HasMany(m => m.MolenTypeAssociations)
                .WithOne(mta => mta.MolenData)
                .HasForeignKey(mta => mta.MolenDataId);
            builder.HasMany(m => m.MolenMakers)
                .WithOne(md => md.MolenData)
                .HasForeignKey(md => md.MolenDataId);
            builder.HasMany(m => m.DisappearedYearInfos)
                .WithOne(dyi => dyi.MolenData)
                .HasForeignKey(dyi => dyi.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(md => md.MolenTBN)
                .WithOne(mtbn => mtbn.MolenData)
                .HasForeignKey<MolenTBN>(mtbn => mtbn.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
