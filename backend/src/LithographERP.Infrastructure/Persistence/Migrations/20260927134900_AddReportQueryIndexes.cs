using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_projects_client_id",
                schema: "projects",
                table: "projects",
                newName: "projects_client_id_idx");

            migrationBuilder.CreateIndex(
                name: "orders_created_at_idx",
                schema: "orders",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "cost_items_expense_date_idx",
                schema: "calculator",
                table: "cost_items",
                column: "expense_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "orders_created_at_idx",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "cost_items_expense_date_idx",
                schema: "calculator",
                table: "cost_items");

            migrationBuilder.RenameIndex(
                name: "projects_client_id_idx",
                schema: "projects",
                table: "projects",
                newName: "IX_projects_client_id");
        }
    }
}
