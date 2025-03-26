using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.BackendNew.Migrations
{
    /// <inheritdoc />
    public partial class Initial8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostedServiceInfos");

            migrationBuilder.CreateTable(
                name: "BackgroundInvocableInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvocableType = table.Column<string>(type: "TEXT", nullable: false),
                    InvocablePayloadType = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundInvocableInfos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundInvocableInfos");

            migrationBuilder.CreateTable(
                name: "HostedServiceInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HostedServicePayloadType = table.Column<string>(type: "TEXT", nullable: false),
                    HostedServiceType = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostedServiceInfos", x => x.Id);
                });
        }
    }
}
