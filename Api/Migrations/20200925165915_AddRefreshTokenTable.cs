using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Api.Migrations
{
    public partial class AddRefreshTokenTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "RefreshTokenHiLoSequence",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SequenceHiLo),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTimeOffset>(nullable: true),
                    token = table.Column<string>(nullable: false),
                    reissue_count = table.Column<int>(nullable: false),
                    person_id = table.Column<long>(nullable: false),
                    app_uuid = table.Column<string>(nullable: false),
                    expired_at = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_token_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_token",
                table: "refresh_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_token_person_id_app_uuid",
                table: "refresh_token",
                columns: new[] { "person_id", "app_uuid" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropSequence(
                name: "RefreshTokenHiLoSequence");
        }
    }
}
