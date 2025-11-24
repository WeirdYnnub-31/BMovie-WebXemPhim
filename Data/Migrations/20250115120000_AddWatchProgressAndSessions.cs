using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webxemphim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchProgressAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm columns vào ViewHits table
            migrationBuilder.AddColumn<double>(
                name: "WatchProgress",
                table: "ViewHits",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "ViewHits",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EpisodeNumber",
                table: "ViewHits",
                type: "int",
                nullable: true);

            // Tạo index cho performance
            migrationBuilder.CreateIndex(
                name: "IX_ViewHits_UserId_MovieId_EpisodeNumber",
                table: "ViewHits",
                columns: new[] { "UserId", "MovieId", "EpisodeNumber" });

            // Tạo UserSessions table
            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCurrentSession = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Tạo indexes cho UserSessions
            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivityAt",
                table: "UserSessions",
                column: "LastActivityAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop UserSessions table
            migrationBuilder.DropTable(
                name: "UserSessions");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_ViewHits_UserId_MovieId_EpisodeNumber",
                table: "ViewHits");

            // Drop columns từ ViewHits
            migrationBuilder.DropColumn(
                name: "WatchProgress",
                table: "ViewHits");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ViewHits");

            migrationBuilder.DropColumn(
                name: "EpisodeNumber",
                table: "ViewHits");
        }
    }
}

