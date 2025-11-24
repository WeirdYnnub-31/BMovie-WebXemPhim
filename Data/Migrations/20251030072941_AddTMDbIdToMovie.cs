using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webxemphim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTMDbIdToMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TMDbId",
                table: "Movies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TMDbId",
                table: "Movies");
        }
    }
}
