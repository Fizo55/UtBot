using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ezaeaz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackedUsername",
                table: "Subscribe",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackedUsername",
                table: "Subscribe");
        }
    }
}
