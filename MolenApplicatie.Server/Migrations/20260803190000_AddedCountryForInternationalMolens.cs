using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MolenApplicatie.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedCountryForInternationalMolens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Land",
                table: "molen_data",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Land",
                table: "molen_data");
        }
    }
}
