using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MolenApplicatie.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedMercatorYToMolenData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MercatorY",
                table: "molen_data",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MercatorY",
                table: "molen_data");
        }
    }
}
