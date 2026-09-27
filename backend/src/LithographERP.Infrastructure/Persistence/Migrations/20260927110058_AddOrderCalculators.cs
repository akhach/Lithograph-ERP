using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCalculators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_calculators",
                schema: "calculator",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_values = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_calculators", x => x.id);
                    table.ForeignKey(
                        name: "order_calculators_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "order_calculators_order_id_fkey",
                        column: x => x.order_id,
                        principalSchema: "orders",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "order_calculators_template_version_id_fkey",
                        column: x => x.template_version_id,
                        principalSchema: "calculator",
                        principalTable: "template_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "order_calculators_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_calculators_created_by",
                schema: "calculator",
                table: "order_calculators",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_calculators_updated_by",
                schema: "calculator",
                table: "order_calculators",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "order_calculators_order_id_uq",
                schema: "calculator",
                table: "order_calculators",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "order_calculators_template_version_id_idx",
                schema: "calculator",
                table: "order_calculators",
                column: "template_version_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_calculators",
                schema: "calculator");
        }
    }
}
