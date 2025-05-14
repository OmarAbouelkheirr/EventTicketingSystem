using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Persons",
                columns: new[] { "PersonID", "Email", "FName", "Image", "LName", "Password", "Role" },
                values: new object[] { 1, "admin@gmail.com", "admin", "", "admin", "6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=", "Admin" });

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "AdminID", "PersonID" },
                values: new object[] { 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "AdminID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Persons",
                keyColumn: "PersonID",
                keyValue: 1);
        }
    }
}
