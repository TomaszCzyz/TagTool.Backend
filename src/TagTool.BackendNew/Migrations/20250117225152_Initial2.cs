using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.BackendNew.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagBaseTaggableItem_Tags_TagsId",
                table: "TagBaseTaggableItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagBaseTaggableItem",
                table: "TagBaseTaggableItem");

            migrationBuilder.DropIndex(
                name: "IX_TagBaseTaggableItem_TagsId",
                table: "TagBaseTaggableItem");

            migrationBuilder.DropColumn(
                name: "TagsId",
                table: "TagBaseTaggableItem");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagBaseTaggableItem",
                table: "TagBaseTaggableItem",
                columns: new[] { "TagBaseId", "TaggableItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_TagBaseTaggableItem_TaggableItemId",
                table: "TagBaseTaggableItem",
                column: "TaggableItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TagBaseTaggableItem_Tags_TagBaseId",
                table: "TagBaseTaggableItem",
                column: "TagBaseId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagBaseTaggableItem_Tags_TagBaseId",
                table: "TagBaseTaggableItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagBaseTaggableItem",
                table: "TagBaseTaggableItem");

            migrationBuilder.DropIndex(
                name: "IX_TagBaseTaggableItem_TaggableItemId",
                table: "TagBaseTaggableItem");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Tags",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<Guid>(
                name: "TagsId",
                table: "TagBaseTaggableItem",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagBaseTaggableItem",
                table: "TagBaseTaggableItem",
                columns: new[] { "TaggableItemId", "TagsId" });

            migrationBuilder.CreateIndex(
                name: "IX_TagBaseTaggableItem_TagsId",
                table: "TagBaseTaggableItem",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_TagBaseTaggableItem_Tags_TagsId",
                table: "TagBaseTaggableItem",
                column: "TagsId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
