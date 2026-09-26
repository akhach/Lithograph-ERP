using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LithographERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "users_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "users_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "roles_created_by_fkey",
                        column: x => x.created_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "roles_updated_by_fkey",
                        column: x => x.updated_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "sessions_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "auth",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "role_permissions_assigned_by_fkey",
                        column: x => x.assigned_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "role_permissions_permission_id_fkey",
                        column: x => x.permission_id,
                        principalSchema: "auth",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "role_permissions_role_id_fkey",
                        column: x => x.role_id,
                        principalSchema: "auth",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "auth",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "user_roles_assigned_by_fkey",
                        column: x => x.assigned_by,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "user_roles_role_id_fkey",
                        column: x => x.role_id,
                        principalSchema: "auth",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "user_roles_user_id_fkey",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "permissions_code_uq",
                schema: "auth",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "permissions_module_idx",
                schema: "auth",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_assigned_by",
                schema: "auth",
                table: "role_permissions",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "role_permissions_permission_id_idx",
                schema: "auth",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_created_by",
                schema: "auth",
                table: "roles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_roles_updated_by",
                schema: "auth",
                table: "roles",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "roles_normalized_name_uq",
                schema: "auth",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sessions_expires_at_idx",
                schema: "auth",
                table: "sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "sessions_token_hash_uq",
                schema: "auth",
                table: "sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sessions_user_id_idx",
                schema: "auth",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by",
                schema: "auth",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "user_roles_role_id_idx",
                schema: "auth",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by",
                schema: "auth",
                table: "users",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_updated_by",
                schema: "auth",
                table: "users",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "users_normalized_username_uq",
                schema: "auth",
                table: "users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");
        }
    }
}
