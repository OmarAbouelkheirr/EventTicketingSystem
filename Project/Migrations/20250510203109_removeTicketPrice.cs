using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVC.Migrations
{
    /// <inheritdoc />
    public partial class removeTicketPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketPrice",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TicketPrice",
                table: "Events",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
