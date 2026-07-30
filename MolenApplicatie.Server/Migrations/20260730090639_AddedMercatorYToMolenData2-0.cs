using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MolenApplicatie.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedMercatorYToMolenData20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "molen_data",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "molen_data",
                newName: "latitude");

            migrationBuilder.RenameColumn(
                name: "MercatorY",
                table: "molen_data",
                newName: "mercator_y");

            migrationBuilder.AlterColumn<string>(
                name: "Ten_Brugge_Nr",
                table: "molen_data",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<double>(
                name: "mercator_y",
                table: "molen_data",
                type: "double",
                nullable: false,
                computedColumnSql: "CASE\n    WHEN latitude IS NOT NULL\n        AND longitude IS NOT NULL\n        AND latitude BETWEEN -90 AND 90\n        AND longitude BETWEEN -180 AND 180\n    THEN (\n        (\n            1 - LN(\n                TAN(\n                    LEAST(\n                        85.05112878,\n                        GREATEST(\n                            -85.05112878,\n                            latitude\n                        )\n                    ) * PI() / 180\n                ) +\n                1 / COS(\n                    LEAST(\n                        85.05112878,\n                        GREATEST(\n                            -85.05112878,\n                            latitude\n                        )\n                    ) * PI() / 180\n                )\n            ) / PI()\n        ) / 2\n    ) * 360\n    ELSE NULL\nEND",
                stored: true,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_molen_data_Ten_Brugge_Nr",
                table: "molen_data",
                column: "Ten_Brugge_Nr");

            migrationBuilder.CreateIndex(
                name: "ix_charge_point_latitude",
                table: "molen_data",
                column: "latitude",
                filter: "\"latitude\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_charge_point_longitude",
                table: "molen_data",
                column: "longitude",
                filter: "\"longitude\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_molen_data_latitude_longitude",
                table: "molen_data",
                columns: new[] { "latitude", "longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_molen_data_Ten_Brugge_Nr",
                table: "molen_data");

            migrationBuilder.DropIndex(
                name: "ix_charge_point_latitude",
                table: "molen_data");

            migrationBuilder.DropIndex(
                name: "ix_charge_point_longitude",
                table: "molen_data");

            migrationBuilder.DropIndex(
                name: "IX_molen_data_latitude_longitude",
                table: "molen_data");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "molen_data",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "molen_data",
                newName: "Latitude");

            migrationBuilder.RenameColumn(
                name: "mercator_y",
                table: "molen_data",
                newName: "MercatorY");

            migrationBuilder.AlterColumn<string>(
                name: "Ten_Brugge_Nr",
                table: "molen_data",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<double>(
                name: "MercatorY",
                table: "molen_data",
                type: "double",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double",
                oldComputedColumnSql: "CASE\n    WHEN latitude IS NOT NULL\n        AND longitude IS NOT NULL\n        AND latitude BETWEEN -90 AND 90\n        AND longitude BETWEEN -180 AND 180\n    THEN (\n        (\n            1 - LN(\n                TAN(\n                    LEAST(\n                        85.05112878,\n                        GREATEST(\n                            -85.05112878,\n                            latitude\n                        )\n                    ) * PI() / 180\n                ) +\n                1 / COS(\n                    LEAST(\n                        85.05112878,\n                        GREATEST(\n                            -85.05112878,\n                            latitude\n                        )\n                    ) * PI() / 180\n                )\n            ) / PI()\n        ) / 2\n    ) * 360\n    ELSE NULL\nEND");
        }
    }
}
