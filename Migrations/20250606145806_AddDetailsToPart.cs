using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkshopManager.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailsToPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogNumber",
                table: "Parts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Parts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Parts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Parts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogNumber",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Parts");
        }
    }
}
