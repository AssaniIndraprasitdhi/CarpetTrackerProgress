using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarpetProgressTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddOverlayUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "overlay_url",
                table: "progress_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_overlay_url",
                table: "orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "overlay_url",
                table: "progress_history");

            migrationBuilder.DropColumn(
                name: "current_overlay_url",
                table: "orders");
        }
    }
}
