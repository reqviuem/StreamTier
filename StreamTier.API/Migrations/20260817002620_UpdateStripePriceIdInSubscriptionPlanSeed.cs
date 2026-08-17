using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamTier.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStripePriceIdInSubscriptionPlanSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "FreePlan",
                column: "StripePriceId",
                value: "price_1U57gt5B4xOxmiDEcyhXD7Nw");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "PremiumPlan",
                column: "StripePriceId",
                value: "price_1U57ic5B4xOxmiDElOLXK9fh");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "StandardPlan",
                column: "StripePriceId",
                value: "price_1U57hw5B4xOxmiDEqD1X3j4r");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "FreePlan",
                column: "StripePriceId",
                value: "prod_V5IHOhTATkiTMx");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "PremiumPlan",
                column: "StripePriceId",
                value: "prod_V5IJ6UpKjjaG71");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "StandardPlan",
                column: "StripePriceId",
                value: "prod_V5IIRM7Z8U74fE");
        }
    }
}
