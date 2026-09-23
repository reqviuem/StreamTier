using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamTier.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueInvoiceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StripeInvoiceId",
                table: "Invoices",
                column: "StripeInvoiceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_StripeInvoiceId",
                table: "Invoices");
        }
    }
}
