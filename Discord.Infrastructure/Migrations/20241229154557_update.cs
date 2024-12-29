using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discord.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TrackedUser",
                table: "Subscribe",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "SubscribedUsers",
                table: "Subscribe",
                type: "numeric(20,0)[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrackedUser",
                table: "Subscribe",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<List<int>>(
                name: "SubscribedUsers",
                table: "Subscribe",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(decimal[]),
                oldType: "numeric(20,0)[]");
        }
    }
}
