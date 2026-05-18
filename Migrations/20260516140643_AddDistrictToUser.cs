using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DZGNCatering.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "Users");
        }
    }
}
