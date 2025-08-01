using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class PlaceConfig : IEntityTypeConfiguration<Place>
    {
        public void Configure(EntityTypeBuilder<Place> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("utf8mb4_general_ci");
            builder.Property(p => p.Province)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("utf8mb4_general_ci");
            builder.Property(p => p.Latitude)
                .HasPrecision(10, 6);
            builder.Property(p => p.Longitude)
                .HasPrecision(10, 6);
            builder.Property(p => p.Population)
                .HasDefaultValue(0);
            builder.HasOne(p => p.Type)
                .WithMany(pt => pt.Places)
                .HasForeignKey(p => p.PlaceTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
