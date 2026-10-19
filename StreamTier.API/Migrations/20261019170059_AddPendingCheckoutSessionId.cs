using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamTier.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingCheckoutSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingCheckoutSessionId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingCheckoutSessionId",
                table: "AspNetUsers");
        }
    }
}
