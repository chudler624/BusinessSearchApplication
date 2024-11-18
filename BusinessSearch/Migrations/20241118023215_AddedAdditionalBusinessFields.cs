using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessSearch.Migrations
{
    /// <inheritdoc />
    public partial class AddedAdditionalBusinessFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BusinessName",
                table: "CrmEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessStatus",
                table: "CrmEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "CrmEntries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "CrmEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "CrmEntries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningStatus",
                table: "CrmEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "CrmEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "CrmEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YelpUrl",
                table: "CrmEntries",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEntries_BusinessName",
                table: "CrmEntries",
                column: "BusinessName");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEntries_DateAdded",
                table: "CrmEntries",
                column: "DateAdded");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEntries_Disposition",
                table: "CrmEntries",
                column: "Disposition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmEntries_BusinessName",
                table: "CrmEntries");

            migrationBuilder.DropIndex(
                name: "IX_CrmEntries_DateAdded",
                table: "CrmEntries");

            migrationBuilder.DropIndex(
                name: "IX_CrmEntries_Disposition",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "BusinessStatus",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "OpeningStatus",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "CrmEntries");

            migrationBuilder.DropColumn(
                name: "YelpUrl",
                table: "CrmEntries");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessName",
                table: "CrmEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
