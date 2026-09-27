using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculatorTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "calculator");

            migrationBuilder.AddColumn<Guid>(
                name: "calculator_template_id",
                schema: "orders",
                table: "order_types",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "templates",
                schema: "calculator",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.id);
                    table.ForeignKey(
                        name: "templates_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "templates_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "template_versions",
                schema: "calculator",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    definition = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_versions", x => x.id);
                    table.CheckConstraint("template_versions_status_ck", "status IN ('draft', 'published', 'retired')");
                    table.CheckConstraint("template_versions_version_number_ck", "version_number >= 1");
                    table.ForeignKey(
                        name: "template_versions_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "template_versions_published_by_fkey",
                        column: x => x.published_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "template_versions_template_id_fkey",
                        column: x => x.template_id,
                        principalSchema: "calculator",
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "order_types_calculator_template_id_idx",
                schema: "orders",
                table: "order_types",
                column: "calculator_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_versions_created_by",
                schema: "calculator",
                table: "template_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_template_versions_published_by",
                schema: "calculator",
                table: "template_versions",
                column: "published_by");

            migrationBuilder.CreateIndex(
                name: "template_versions_one_draft_uq",
                schema: "calculator",
                table: "template_versions",
                column: "template_id",
                unique: true,
                filter: "status = 'draft'");

            migrationBuilder.CreateIndex(
                name: "template_versions_template_id_version_number_uq",
                schema: "calculator",
                table: "template_versions",
                columns: new[] { "template_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_templates_created_by",
                schema: "calculator",
                table: "templates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_templates_updated_by",
                schema: "calculator",
                table: "templates",
                column: "updated_by");

            migrationBuilder.AddForeignKey(
                name: "order_types_calculator_template_id_fkey",
                schema: "orders",
                table: "order_types",
                column: "calculator_template_id",
                principalSchema: "calculator",
                principalTable: "templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX templates_name_ci_uq ON calculator.templates (lower(name));

                CREATE FUNCTION calculator.prevent_template_version_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                  IF TG_OP = 'DELETE' THEN
                    IF OLD.status IN ('published', 'retired') THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;
                    RETURN OLD;
                  END IF;

                  IF OLD.status IN ('published', 'retired') THEN
                    IF NEW.definition IS DISTINCT FROM OLD.definition
                       OR NEW.version_number IS DISTINCT FROM OLD.version_number
                       OR NEW.template_id IS DISTINCT FROM OLD.template_id
                       OR NEW.created_at IS DISTINCT FROM OLD.created_at
                       OR NEW.created_by IS DISTINCT FROM OLD.created_by
                       OR NEW.id IS DISTINCT FROM OLD.id THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;

                    IF OLD.status = 'retired' AND NEW.status IS DISTINCT FROM 'retired' THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;

                    IF OLD.status = 'published' AND NEW.status NOT IN ('published', 'retired') THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;

                    IF OLD.published_at IS NOT NULL AND NEW.published_at IS DISTINCT FROM OLD.published_at THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;

                    IF OLD.published_by IS NOT NULL AND NEW.published_by IS DISTINCT FROM OLD.published_by THEN
                      RAISE EXCEPTION 'template version immutable' USING ERRCODE = '23514';
                    END IF;
                  END IF;

                  RETURN NEW;
                END;
                $$;

                CREATE TRIGGER template_versions_immutable
                BEFORE UPDATE OR DELETE ON calculator.template_versions
                FOR EACH ROW
                EXECUTE FUNCTION calculator.prevent_template_version_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS template_versions_immutable ON calculator.template_versions;
                DROP FUNCTION IF EXISTS calculator.prevent_template_version_mutation();
                DROP INDEX IF EXISTS calculator.templates_name_ci_uq;
                """);

            migrationBuilder.DropForeignKey(
                name: "order_types_calculator_template_id_fkey",
                schema: "orders",
                table: "order_types");

            migrationBuilder.DropTable(
                name: "template_versions",
                schema: "calculator");

            migrationBuilder.DropTable(
                name: "templates",
                schema: "calculator");

            migrationBuilder.DropIndex(
                name: "order_types_calculator_template_id_idx",
                schema: "orders",
                table: "order_types");

            migrationBuilder.DropColumn(
                name: "calculator_template_id",
                schema: "orders",
                table: "order_types");
        }
    }
}
