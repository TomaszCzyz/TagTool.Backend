using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.BackendNew.Migrations
{
    /// <inheritdoc />
    public partial class Initial9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagQueryPart_CronTriggeredInvocableInfos_CronTriggeredInvocableInfoId",
                table: "TagQueryPart");

            migrationBuilder.DropTable(
                name: "BackgroundInvocableInfos");

            migrationBuilder.DropTable(
                name: "CronTriggeredInvocableInfos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventTriggeredInvocableInfos",
                table: "EventTriggeredInvocableInfos");

            migrationBuilder.RenameTable(
                name: "EventTriggeredInvocableInfos",
                newName: "InvocableInfos");

            migrationBuilder.AlterColumn<string>(
                name: "InvocableType",
                table: "InvocableInfos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "InvocablePayloadType",
                table: "InvocableInfos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "InvocableInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "InvocableInfos",
                type: "TEXT",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "InvocableInfos",
                type: "TEXT",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InvocableId",
                table: "InvocableInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedOnUtc",
                table: "InvocableInfos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InvocableInfos",
                table: "InvocableInfos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagQueryPart_InvocableInfos_CronTriggeredInvocableInfoId",
                table: "TagQueryPart",
                column: "CronTriggeredInvocableInfoId",
                principalTable: "InvocableInfos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagQueryPart_InvocableInfos_CronTriggeredInvocableInfoId",
                table: "TagQueryPart");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InvocableInfos",
                table: "InvocableInfos");

            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "InvocableInfos");

            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "InvocableInfos");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "InvocableInfos");

            migrationBuilder.DropColumn(
                name: "InvocableId",
                table: "InvocableInfos");

            migrationBuilder.DropColumn(
                name: "ModifiedOnUtc",
                table: "InvocableInfos");

            migrationBuilder.RenameTable(
                name: "InvocableInfos",
                newName: "EventTriggeredInvocableInfos");

            migrationBuilder.AlterColumn<string>(
                name: "InvocableType",
                table: "EventTriggeredInvocableInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvocablePayloadType",
                table: "EventTriggeredInvocableInfos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventTriggeredInvocableInfos",
                table: "EventTriggeredInvocableInfos",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BackgroundInvocableInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvocablePayloadType = table.Column<string>(type: "TEXT", nullable: false),
                    InvocableType = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundInvocableInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CronTriggeredInvocableInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    InvocablePayloadType = table.Column<string>(type: "TEXT", nullable: false),
                    InvocableType = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CronTriggeredInvocableInfos", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_TagQueryPart_CronTriggeredInvocableInfos_CronTriggeredInvocableInfoId",
                table: "TagQueryPart",
                column: "CronTriggeredInvocableInfoId",
                principalTable: "CronTriggeredInvocableInfos",
                principalColumn: "Id");
        }
    }
}
