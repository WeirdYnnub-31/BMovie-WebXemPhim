using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webxemphim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteForUserInventoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_UserInventoryItems_Movies_MovieId",
                table: "UserInventoryItems");

            // Recreate with cascade delete
            migrationBuilder.AddForeignKey(
                name: "FK_UserInventoryItems_Movies_MovieId",
                table: "UserInventoryItems",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop cascade foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_UserInventoryItems_Movies_MovieId",
                table: "UserInventoryItems");

            // Restore original foreign key without cascade
            migrationBuilder.AddForeignKey(
                name: "FK_UserInventoryItems_Movies_MovieId",
                table: "UserInventoryItems",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id");
        }
    }
}

