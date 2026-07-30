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
                .HasForeignKey(ai => ai.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(m => m.Images)
                .WithOne(mi => mi.MolenData)
                .HasForeignKey(mi => mi.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(m => m.MolenTypeAssociations)
                .WithOne(mta => mta.MolenData)
                .HasForeignKey(mta => mta.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(m => m.MolenMakers)
                .WithOne(md => md.MolenData)
                .HasForeignKey(md => md.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(m => m.DisappearedYearInfos)
                .WithOne(dyi => dyi.MolenData)
                .HasForeignKey(dyi => dyi.MolenDataId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(md => md.MolenTBN)
                .WithOne(mtbn => mtbn.MolenData)
                .HasForeignKey<MolenData>(mtbn => mtbn.MolenTBNId);

            builder.Property(x => x.Latitude).HasColumnName("latitude");
            builder.Property(x => x.Longitude).HasColumnName("longitude");
            builder.HasIndex(x => new
            {
                x.Latitude,
                x.Longitude
            });

            builder.HasIndex(x => x.Latitude)
                .HasDatabaseName("ix_charge_point_latitude")
                .HasFilter("\"latitude\" IS NOT NULL");

            builder.HasIndex(x => x.Longitude)
                .HasDatabaseName("ix_charge_point_longitude")
                .HasFilter("\"longitude\" IS NOT NULL");

            builder.Property(x => x.MercatorY)
                .HasColumnName("mercator_y")
                .HasComputedColumnSql(
                    MercatorYComputedColumnSql,
                    stored: true);
        }

        private static readonly string MercatorYComputedColumnSql =
            """
            CASE
                WHEN latitude IS NOT NULL
                    AND longitude IS NOT NULL
                    AND latitude BETWEEN -90 AND 90
                    AND longitude BETWEEN -180 AND 180
                THEN (
                    (
                        1 - LN(
                            TAN(
                                LEAST(
                                    85.05112878,
                                    GREATEST(
                                        -85.05112878,
                                        latitude
                                    )
                                ) * PI() / 180
                            ) +
                            1 / COS(
                                LEAST(
                                    85.05112878,
                                    GREATEST(
                                        -85.05112878,
                                        latitude
                                    )
                                ) * PI() / 180
                            )
                        ) / PI()
                    ) / 2
                ) * 360
                ELSE NULL
            END
            """.ReplaceLineEndings("\n");
    }
}
