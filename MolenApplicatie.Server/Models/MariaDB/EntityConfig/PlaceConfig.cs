using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class PlaceConfig : IEntityTypeConfiguration<Place>
    {
        public void Configure(EntityTypeBuilder<Place> builder)
        {
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("utf8mb4_general_ci");

            builder.HasIndex(p => p.Name)
                .IsUnique();
        }
    }
}
