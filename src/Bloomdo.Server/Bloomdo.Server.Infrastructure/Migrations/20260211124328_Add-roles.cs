using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addroles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Permission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Permission", "Role", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:view", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:edit", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:create", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:edit", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:delete", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "blocks:manage", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:view", 0, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:view", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:edit", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:create", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:edit", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:delete", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "blocks:manage", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:view", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "premium:access", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:export", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:view", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:edit", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:create", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:edit", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:delete", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "blocks:manage", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:view", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "premium:access", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:export", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000026"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "users:view", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000027"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "users:manage", 2, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000028"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:view", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000029"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "profile:edit", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000030"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:create", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000031"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:edit", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000032"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "goals:delete", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000033"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "blocks:manage", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000034"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:view", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000035"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "premium:access", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000036"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "stats:export", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000037"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "users:view", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000038"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "users:manage", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000039"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "roles:manage", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000040"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "settings:manage", 3, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000041"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "analytics:view", 3, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_Role_Permission",
                table: "RolePermissions",
                columns: new[] { "Role", "Permission" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Accounts");
        }
    }
}
