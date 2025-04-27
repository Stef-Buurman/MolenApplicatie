using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class AddedImageConfig : IEntityTypeConfiguration<AddedImage>
    {
        public void Configure(EntityTypeBuilder<AddedImage> builder)
        {
            builder.HasOne(ai => ai.MolenData)          
                 .WithMany(md => md.AddedImages)      
                 .HasForeignKey(ai => ai.MolenDataId)
                 .IsRequired();

        }
    }
}

