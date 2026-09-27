using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "projects");

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.CheckConstraint("projects_status_ck", "status IN ('draft', 'active', 'on_hold', 'completed', 'cancelled')");
                    table.ForeignKey(
                        name: "projects_client_id_fkey",
                        column: x => x.client_id,
                        principalSchema: "clients",
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "projects_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "projects_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                schema: "projects",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.project_id, x.employee_id, x.project_role });
                    table.CheckConstraint("project_members_role_ck", "project_role IN ('owner', 'assignee', 'participant', 'observer')");
                    table.ForeignKey(
                        name: "project_members_assigned_by_fkey",
                        column: x => x.assigned_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "project_members_employee_id_fkey",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "project_members_project_id_fkey",
                        column: x => x.project_id,
                        principalSchema: "projects",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_project_members_assigned_by",
                schema: "projects",
                table: "project_members",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_project_members_employee_id",
                schema: "projects",
                table: "project_members",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "project_members_one_assignee_uq",
                schema: "projects",
                table: "project_members",
                column: "project_id",
                unique: true,
                filter: "project_role = 'assignee'");

            migrationBuilder.CreateIndex(
                name: "IX_projects_client_id",
                schema: "projects",
                table: "projects",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_created_by",
                schema: "projects",
                table: "projects",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_projects_updated_by",
                schema: "projects",
                table: "projects",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "projects_business_id_uq",
                schema: "projects",
                table: "projects",
                column: "business_id",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX project_members_one_owner_uq
                ON projects.project_members (project_id)
                WHERE project_role = 'owner';

                CREATE OR REPLACE FUNCTION projects.next_project_business_id(target_year integer)
                RETURNS bigint
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    sequence_name text;
                BEGIN
                    IF target_year < 1 OR target_year > 9999 THEN
                        RAISE EXCEPTION 'invalid project numbering year';
                    END IF;
                    sequence_name := format('projects.project_business_id_%s_seq', target_year);
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
                DROP FUNCTION IF EXISTS projects.next_project_business_id(integer);
                DROP INDEX IF EXISTS projects.project_members_one_owner_uq;
                DO $$
                DECLARE sequence_name text;
                BEGIN
                  FOR sequence_name IN
                    SELECT schemaname || '.' || sequencename
                    FROM pg_sequences
                    WHERE schemaname = 'projects' AND sequencename LIKE 'project_business_id_%'
                  LOOP
                    EXECUTE format('DROP SEQUENCE IF EXISTS %s', sequence_name);
                  END LOOP;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "project_members",
                schema: "projects");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "projects");
        }
    }
}
