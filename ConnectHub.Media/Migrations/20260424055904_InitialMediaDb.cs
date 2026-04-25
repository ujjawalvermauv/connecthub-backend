using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConnectHub.Media.Migrations
{
    /// <inheritdoc />
    public partial class InitialMediaDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    FileId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSizeKb = table.Column<long>(type: "bigint", nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.FileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ExpiresAt",
                table: "MediaFiles",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_MessageId",
                table: "MediaFiles",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_RoomId",
                table: "MediaFiles",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedAt",
                table: "MediaFiles",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaFiles");
        }
    }
}
