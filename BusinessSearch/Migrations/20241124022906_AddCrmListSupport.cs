using Microsoft.EntityFrameworkCore.Migrations;

namespace BusinessSearch.Migrations
{
    public partial class AddCrmListSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create TeamMembers table
            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            // Create CrmLists table
            migrationBuilder.CreateTable(
                name: "CrmLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmLists_TeamMembers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create CrmEntryList junction table
            migrationBuilder.CreateTable(
                name: "CrmEntryLists",
                columns: table => new
                {
                    CrmEntryId = table.Column<int>(type: "int", nullable: false),
                    CrmListId = table.Column<int>(type: "int", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEntryLists", x => new { x.CrmEntryId, x.CrmListId });
                    table.ForeignKey(
                        name: "FK_CrmEntryLists_CrmEntries_CrmEntryId",
                        column: x => x.CrmEntryId,
                        principalTable: "CrmEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmEntryLists_CrmLists_CrmListId",
                        column: x => x.CrmListId,
                        principalTable: "CrmLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_CrmLists_Name",
                table: "CrmLists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CrmLists_AssignedToId",
                table: "CrmLists",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEntryLists_CrmListId",
                table: "CrmEntryLists",
                column: "CrmListId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_Email",
                table: "TeamMembers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CrmEntryLists");
            migrationBuilder.DropTable(name: "CrmLists");
            migrationBuilder.DropTable(name: "TeamMembers");
        }
    }
}
