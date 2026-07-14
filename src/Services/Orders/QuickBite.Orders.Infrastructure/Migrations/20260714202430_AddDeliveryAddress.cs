using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickBite.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressCity",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressCountry",
                table: "Orders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressLine1",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressLine2",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressPostalCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressState",
                table: "Orders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAddressCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressCountry",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressLine1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressLine2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressPostalCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressState",
                table: "Orders");
        }
    }
}
