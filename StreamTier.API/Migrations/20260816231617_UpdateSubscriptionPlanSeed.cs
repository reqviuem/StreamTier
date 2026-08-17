using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamTier.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPlanSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "FreePlan",
                column: "StripePriceId",
                value: "22");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "PremiumPlan",
                column: "StripePriceId",
                value: "12");

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: "StandardPlan",
                column: "StripePriceId",
                value: "38");
        }
    }
}
