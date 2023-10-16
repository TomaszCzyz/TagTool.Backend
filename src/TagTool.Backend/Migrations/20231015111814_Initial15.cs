﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagTool.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventTasks",
                columns: table => new
                {
                    TaskId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionId = table.Column<string>(type: "TEXT", nullable: false),
                    ActionAttributes = table.Column<string>(type: "TEXT", nullable: false),
                    Events = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTasks", x => x.TaskId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventTasks");
        }
    }
}
