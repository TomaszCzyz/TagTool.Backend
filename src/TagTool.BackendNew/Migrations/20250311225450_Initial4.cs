using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.BackendNew.Migrations
{
    /// <inheritdoc />
    public partial class Initial4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagQuery",
                table: "CronTriggeredInvocableInfos");

            migrationBuilder.CreateTable(
                name: "TagQueryPart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    CronTriggeredInvocableInfoId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagQueryPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagQueryPart_CronTriggeredInvocableInfos_CronTriggeredInvocableInfoId",
                        column: x => x.CronTriggeredInvocableInfoId,
                        principalTable: "CronTriggeredInvocableInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TagQueryPart_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagQueryPart_CronTriggeredInvocableInfoId",
                table: "TagQueryPart",
                column: "CronTriggeredInvocableInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_TagQueryPart_TagId",
                table: "TagQueryPart",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagQueryPart");

            migrationBuilder.AddColumn<string>(
                name: "TagQuery",
                table: "CronTriggeredInvocableInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
