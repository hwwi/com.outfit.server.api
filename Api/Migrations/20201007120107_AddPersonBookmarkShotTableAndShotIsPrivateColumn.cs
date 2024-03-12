using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Api.Migrations
{
    public partial class AddPersonBookmarkShotTableAndShotIsPrivateColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "shot",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "person_bookmark_shot",
                columns: table => new
                {
                    person_id = table.Column<long>(nullable: false),
                    shot_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_bookmark_shot", x => new { x.person_id, x.shot_id });
                    table.ForeignKey(
                        name: "FK_person_bookmark_shot_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_person_bookmark_shot_shot_shot_id",
                        column: x => x.shot_id,
                        principalTable: "shot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_person_bookmark_shot_shot_id",
                table: "person_bookmark_shot",
                column: "shot_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_bookmark_shot");

            migrationBuilder.DropColumn(
                name: "is_private",
                table: "shot");
        }
    }
}
