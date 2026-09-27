using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "projects_created_at_idx",
                schema: "projects",
                table: "projects",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "orders_priority_idx",
                schema: "orders",
                table: "orders",
                column: "priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "projects_created_at_idx",
                schema: "projects",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "orders_priority_idx",
                schema: "orders",
                table: "orders");
        }
    }
}
