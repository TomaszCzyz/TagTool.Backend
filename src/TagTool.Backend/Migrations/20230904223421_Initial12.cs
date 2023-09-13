using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagSynonymsGroup_TagSynonymsGroupId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagsHierarchy_TagsHierarchyId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_TagsHierarchy_Tags_BaseTagId",
                table: "TagsHierarchy");

            migrationBuilder.DropTable(
                name: "AssociationDescriptions");

            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropIndex(
                name: "IX_Tags_TagsHierarchyId",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagSynonymsGroup",
                table: "TagSynonymsGroup");

            migrationBuilder.DropColumn(
                name: "TagsHierarchyId",
                table: "Tags");

            migrationBuilder.RenameTable(
                name: "TagSynonymsGroup",
                newName: "TagSynonymsGroups");

            migrationBuilder.RenameColumn(
                name: "BaseTagId",
                table: "TagsHierarchy",
                newName: "ParentGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TagsHierarchy_BaseTagId",
                table: "TagsHierarchy",
                newName: "IX_TagsHierarchy_ParentGroupId");

            migrationBuilder.RenameColumn(
                name: "SynonymGroupName",
                table: "TagSynonymsGroups",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "TagsHierarchyId",
                table: "TagSynonymsGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagSynonymsGroups",
                table: "TagSynonymsGroups",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TagSynonymsGroups_TagsHierarchyId",
                table: "TagSynonymsGroups",
                column: "TagsHierarchyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagSynonymsGroups_TagSynonymsGroupId",
                table: "Tags",
                column: "TagSynonymsGroupId",
                principalTable: "TagSynonymsGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagsHierarchy_TagSynonymsGroups_ParentGroupId",
                table: "TagsHierarchy",
                column: "ParentGroupId",
                principalTable: "TagSynonymsGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TagSynonymsGroups_TagsHierarchy_TagsHierarchyId",
                table: "TagSynonymsGroups",
                column: "TagsHierarchyId",
                principalTable: "TagsHierarchy",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagSynonymsGroups_TagSynonymsGroupId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_TagsHierarchy_TagSynonymsGroups_ParentGroupId",
                table: "TagsHierarchy");

            migrationBuilder.DropForeignKey(
                name: "FK_TagSynonymsGroups_TagsHierarchy_TagsHierarchyId",
                table: "TagSynonymsGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagSynonymsGroups",
                table: "TagSynonymsGroups");

            migrationBuilder.DropIndex(
                name: "IX_TagSynonymsGroups_TagsHierarchyId",
                table: "TagSynonymsGroups");

            migrationBuilder.DropColumn(
                name: "TagsHierarchyId",
                table: "TagSynonymsGroups");

            migrationBuilder.RenameTable(
                name: "TagSynonymsGroups",
                newName: "TagSynonymsGroup");

            migrationBuilder.RenameColumn(
                name: "ParentGroupId",
                table: "TagsHierarchy",
                newName: "BaseTagId");

            migrationBuilder.RenameIndex(
                name: "IX_TagsHierarchy_ParentGroupId",
                table: "TagsHierarchy",
                newName: "IX_TagsHierarchy_BaseTagId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "TagSynonymsGroup",
                newName: "SynonymGroupName");

            migrationBuilder.AddColumn<int>(
                name: "TagsHierarchyId",
                table: "Tags",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagSynonymsGroup",
                table: "TagSynonymsGroup",
                column: "Id");

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
                    table.ForeignKey(
                        name: "FK_Associations_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssociationDescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociationType = table.Column<int>(type: "INTEGER", nullable: false),
                    TagAssociationsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationDescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationDescriptions_Associations_TagAssociationsId",
                        column: x => x.TagAssociationsId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssociationDescriptions_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1000,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1001,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1002,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1003,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1004,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1005,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1006,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2001,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2002,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2003,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2004,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2005,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2006,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2007,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2008,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2009,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2010,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2011,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2012,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 3002,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 3003,
                column: "TagsHierarchyId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagsHierarchyId",
                table: "Tags",
                column: "TagsHierarchyId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagSynonymsGroup_TagSynonymsGroupId",
                table: "Tags",
                column: "TagSynonymsGroupId",
                principalTable: "TagSynonymsGroup",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagsHierarchy_TagsHierarchyId",
                table: "Tags",
                column: "TagsHierarchyId",
                principalTable: "TagsHierarchy",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagsHierarchy_Tags_BaseTagId",
                table: "TagsHierarchy",
                column: "BaseTagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
