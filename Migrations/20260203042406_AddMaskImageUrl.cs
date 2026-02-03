using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarpetProgressTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddMaskImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mask_image_url",
                table: "orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mask_image_url",
                table: "orders");
        }
    }
}
