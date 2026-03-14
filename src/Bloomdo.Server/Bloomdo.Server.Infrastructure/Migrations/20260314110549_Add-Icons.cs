using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bloomdo.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "ActivityItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "#7E57C2");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "ActivityItems",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "✨");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "ActivityItems");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "ActivityItems");
        }
    }
}
