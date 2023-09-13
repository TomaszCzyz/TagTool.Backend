using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "Popularity",
                table: "TaggableFolders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "Popularity",
                table: "TaggableFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "TaggableFolders");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "TaggableFiles");
        }
    }
}
