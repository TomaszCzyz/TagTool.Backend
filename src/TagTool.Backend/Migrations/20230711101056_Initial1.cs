using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TagTool.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaggableFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaggableFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaggableFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaggableFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagSynonymsGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SynonymGroupName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagSynonymsGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssociationDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagAssociationsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociationType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationDescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagBaseTaggableItem",
                columns: table => new
                {
                    TaggedItemsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagBaseTaggableItem", x => new { x.TaggedItemsId, x.TagsId });
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormattedName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Added = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Deleted = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    TagSynonymsGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    TagsHierarchyId = table.Column<int>(type: "INTEGER", nullable: true),
                    Begin = table.Column<int>(type: "INTEGER", nullable: true),
                    End = table.Column<int>(type: "INTEGER", nullable: true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Month = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_TagSynonymsGroup_TagSynonymsGroupId",
                        column: x => x.TagSynonymsGroupId,
                        principalTable: "TagSynonymsGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TagsHierarchy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagsHierarchy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagsHierarchy_Tags_BaseTagId",
                        column: x => x.BaseTagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Added", "DayOfWeek", "Deleted", "Discriminator", "FormattedName", "Modified", "TagSynonymsGroupId", "TagsHierarchyId" },
                values: new object[,]
                {
                    { 1000, null, 0, null, "DayTag", "DayTag:Sunday", null, null, null },
                    { 1001, null, 1, null, "DayTag", "DayTag:Monday", null, null, null },
                    { 1002, null, 2, null, "DayTag", "DayTag:Tuesday", null, null, null },
                    { 1003, null, 3, null, "DayTag", "DayTag:Wednesday", null, null, null },
                    { 1004, null, 4, null, "DayTag", "DayTag:Thursday", null, null, null },
                    { 1005, null, 5, null, "DayTag", "DayTag:Friday", null, null, null },
                    { 1006, null, 6, null, "DayTag", "DayTag:Saturday", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Added", "Deleted", "Discriminator", "FormattedName", "Modified", "Month", "TagSynonymsGroupId", "TagsHierarchyId" },
                values: new object[,]
                {
                    { 2001, null, null, "MonthTag", "MonthTag:January", null, 1, null, null },
                    { 2002, null, null, "MonthTag", "MonthTag:February", null, 2, null, null },
                    { 2003, null, null, "MonthTag", "MonthTag:March", null, 3, null, null },
                    { 2004, null, null, "MonthTag", "MonthTag:April", null, 4, null, null },
                    { 2005, null, null, "MonthTag", "MonthTag:May", null, 5, null, null },
                    { 2006, null, null, "MonthTag", "MonthTag:June", null, 6, null, null },
                    { 2007, null, null, "MonthTag", "MonthTag:July", null, 7, null, null },
                    { 2008, null, null, "MonthTag", "MonthTag:August", null, 8, null, null },
                    { 2009, null, null, "MonthTag", "MonthTag:September", null, 9, null, null },
                    { 2010, null, null, "MonthTag", "MonthTag:October", null, 10, null, null },
                    { 2011, null, null, "MonthTag", "MonthTag:November", null, 11, null, null },
                    { 2012, null, null, "MonthTag", "MonthTag:December", null, 12, null, null }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Added", "Deleted", "Discriminator", "FormattedName", "Modified", "TagSynonymsGroupId", "TagsHierarchyId", "Type" },
                values: new object[,]
                {
                    { 3002, null, null, "ItemTypeTag", "ItemTypeTag:TaggableFile", null, null, null, "TagTool.Backend.Models.TaggableFile, TagTool.Backend, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" },
                    { 3003, null, null, "ItemTypeTag", "ItemTypeTag:TaggableFolder", null, null, null, "TagTool.Backend.Models.TaggableFolder, TagTool.Backend, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationDescriptions_TagAssociationsId",
                table: "AssociationDescriptions",
                column: "TagAssociationsId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationDescriptions_TagId",
                table: "AssociationDescriptions",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_TagId",
                table: "Associations",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagBaseTaggableItem_TagsId",
                table: "TagBaseTaggableItem",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_TaggableFiles_Path",
                table: "TaggableFiles",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaggableFolders_Path",
                table: "TaggableFolders",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FormattedName",
                table: "Tags",
                column: "FormattedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagsHierarchyId",
                table: "Tags",
                column: "TagsHierarchyId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagSynonymsGroupId",
                table: "Tags",
                column: "TagSynonymsGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TagsHierarchy_BaseTagId",
                table: "TagsHierarchy",
                column: "BaseTagId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationDescriptions_Associations_TagAssociationsId",
                table: "AssociationDescriptions",
                column: "TagAssociationsId",
                principalTable: "Associations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssociationDescriptions_Tags_TagId",
                table: "AssociationDescriptions",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Associations_Tags_TagId",
                table: "Associations",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TagBaseTaggableItem_Tags_TagsId",
                table: "TagBaseTaggableItem",
                column: "TagsId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagsHierarchy_TagsHierarchyId",
                table: "Tags",
                column: "TagsHierarchyId",
                principalTable: "TagsHierarchy",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagsHierarchy_Tags_BaseTagId",
                table: "TagsHierarchy");

            migrationBuilder.DropTable(
                name: "AssociationDescriptions");

            migrationBuilder.DropTable(
                name: "TagBaseTaggableItem");

            migrationBuilder.DropTable(
                name: "TaggableFiles");

            migrationBuilder.DropTable(
                name: "TaggableFolders");

            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "TagSynonymsGroup");

            migrationBuilder.DropTable(
                name: "TagsHierarchy");
        }
    }
}
