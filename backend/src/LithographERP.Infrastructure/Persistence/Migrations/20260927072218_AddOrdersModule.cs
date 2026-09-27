using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.CreateTable(
                name: "order_types",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_types", x => x.id);
                    table.ForeignKey(
                        name: "order_types_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "order_types_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "draft"),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "normal"),
                    selling_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    cost_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    preview_image_path = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("orders_cost_price_ck", "cost_price >= 0");
                    table.CheckConstraint("orders_priority_ck", "priority IN ('low', 'normal', 'high', 'urgent')");
                    table.CheckConstraint("orders_selling_price_ck", "selling_price >= 0");
                    table.CheckConstraint("orders_status_ck", "status IN ('draft', 'active', 'on_hold', 'completed', 'cancelled')");
                    table.ForeignKey(
                        name: "orders_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "orders_order_type_id_fkey",
                        column: x => x.order_type_id,
                        principalSchema: "orders",
                        principalTable: "order_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "orders_project_id_fkey",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "orders_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_items",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "checklist_items_order_id_fkey",
                        column: x => x.order_id,
                        principalSchema: "orders",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "folder_links",
                schema: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    path = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folder_links", x => x.id);
                    table.ForeignKey(
                        name: "folder_links_order_id_fkey",
                        column: x => x.order_id,
                        principalSchema: "orders",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "checklist_items_order_id_sort_order_idx",
                schema: "orders",
                table: "checklist_items",
                columns: new[] { "order_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "folder_links_order_id_sort_order_idx",
                schema: "orders",
                table: "folder_links",
                columns: new[] { "order_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_order_types_created_by",
                schema: "orders",
                table: "order_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_types_updated_by",
                schema: "orders",
                table: "order_types",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by",
                schema: "orders",
                table: "orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_orders_updated_by",
                schema: "orders",
                table: "orders",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "orders_business_id_uq",
                schema: "orders",
                table: "orders",
                column: "business_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "orders_deadline_idx",
                schema: "orders",
                table: "orders",
                column: "deadline");

            migrationBuilder.CreateIndex(
                name: "orders_order_type_id_idx",
                schema: "orders",
                table: "orders",
                column: "order_type_id");

            migrationBuilder.CreateIndex(
                name: "orders_project_id_idx",
                schema: "orders",
                table: "orders",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "orders_status_idx",
                schema: "orders",
                table: "orders",
                column: "status");

            // calculator_template_id is omitted until calculator.templates exists.
            // The Calculator migration adds that nullable column and its foreign key.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX order_types_name_ci_uq ON orders.order_types (lower(name));

                CREATE OR REPLACE FUNCTION orders.next_order_business_id(target_year integer)
                RETURNS bigint
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    sequence_name text;
                BEGIN
                    IF target_year < 1 OR target_year > 9999 THEN
                        RAISE EXCEPTION 'invalid order numbering year';
                    END IF;
                    sequence_name := format('orders.order_business_id_%s_seq', target_year);
                    EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %s', sequence_name);
                    RETURN nextval(sequence_name::regclass);
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS orders.next_order_business_id(integer);
                DO $$
                DECLARE sequence_name text;
                BEGIN
                  FOR sequence_name IN
                    SELECT schemaname || '.' || sequencename
                    FROM pg_sequences
                    WHERE schemaname = 'orders' AND sequencename LIKE 'order_business_id_%'
                  LOOP
                    EXECUTE format('DROP SEQUENCE IF EXISTS %s', sequence_name);
                  END LOOP;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "checklist_items",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "folder_links",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "orders");

            migrationBuilder.DropTable(
                name: "order_types",
                schema: "orders");
        }
    }
}
