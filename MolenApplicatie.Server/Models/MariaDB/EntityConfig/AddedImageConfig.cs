using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class AddedImageConfig : IEntityTypeConfiguration<AddedImage>
    {
        public void Configure(EntityTypeBuilder<AddedImage> builder)
        {
            builder.HasKey(ai => ai.Id);
            builder.Property(ai => ai.MolenDataId)
                .IsRequired();
            builder.HasOne(ai => ai.MolenData)
                 .WithMany(md => md.AddedImages)
                 .HasForeignKey(ai => ai.MolenDataId)
                 .IsRequired();

        }
    }
}

