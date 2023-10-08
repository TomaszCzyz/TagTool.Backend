using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial14 : Migration
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

            migrationBuilder.RenameColumn(
                name: "TagsId",
                table: "TagBaseTaggableItem",
                newName: "TagBaseId");

            migrationBuilder.RenameColumn(
                name: "TaggedItemsId",
                table: "TagBaseTaggableItem",
                newName: "TaggableItemId");

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

            migrationBuilder.RenameColumn(
                name: "TaggableItemId",
                table: "TagBaseTaggableItem",
                newName: "TaggedItemsId");

            migrationBuilder.RenameColumn(
                name: "TagBaseId",
                table: "TagBaseTaggableItem",
                newName: "TagsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagBaseTaggableItem",
                table: "TagBaseTaggableItem",
                columns: new[] { "TaggedItemsId", "TagsId" });

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
