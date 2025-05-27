using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkshopManager.Migrations
{
    /// <inheritdoc />
    public partial class MakeAssignedMechanicIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_AspNetUsers_AssignedMechanicId",
                table: "ServiceOrders");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedMechanicId",
                table: "ServiceOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_AspNetUsers_AssignedMechanicId",
                table: "ServiceOrders",
                column: "AssignedMechanicId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrders_AspNetUsers_AssignedMechanicId",
                table: "ServiceOrders");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedMechanicId",
                table: "ServiceOrders",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrders_AspNetUsers_AssignedMechanicId",
                table: "ServiceOrders",
                column: "AssignedMechanicId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
