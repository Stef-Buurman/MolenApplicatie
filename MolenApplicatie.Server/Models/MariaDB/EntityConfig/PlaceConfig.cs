using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MolenApplicatie.Server.Models.MariaDB.EntityConfig
{
    internal class PlaceConfig : IEntityTypeConfiguration<Place>
    {
        public void Configure(EntityTypeBuilder<Place> builder)
        {
            builder.HasAlternateKey(p => p.Name);
            builder.Property(p => p.Name)
                .IsRequired();
        }
    }
}
