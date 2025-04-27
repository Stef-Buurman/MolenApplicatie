using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class DisappearedYearInfoConfig : IEntityTypeConfiguration<DisappearedYearInfo>
    {
        public void Configure(EntityTypeBuilder<DisappearedYearInfo> builder)
        {
            builder.HasKey(vyi => vyi.Id);
            builder.Property(vyi => vyi.Year)
                .IsRequired();
            builder.Property(vyi => vyi.MolenDataId)
                .IsRequired();
            builder.HasOne(vyi => vyi.MolenData)
                .WithMany(md => md.DisappearedYearInfos)
                .HasForeignKey(vyi => vyi.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
