using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Database.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    CurrentPlayer = table.Column<int>(nullable: false),
                    CurrentPosition = table.Column<int>(nullable: false),
                    Fen = table.Column<string>(nullable: true),
                    InitialPosition = table.Column<string>(nullable: true),
                    Variant = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Turns",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    GameID = table.Column<Guid>(nullable: false),
                    MoveNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turns", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Turns_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Moves",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    DisplayString = table.Column<string>(nullable: true),
                    IsJump = table.Column<bool>(nullable: true),
                    Move = table.Column<string>(nullable: true),
                    PieceTypeMoved = table.Column<int>(nullable: false),
                    Player = table.Column<int>(nullable: false),
                    ResultingFen = table.Column<string>(nullable: true),
                    TurnID = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moves", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Moves_Turns_TurnID",
                        column: x => x.TurnID,
                        principalTable: "Turns",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Moves_TurnID",
                table: "Moves",
                column: "TurnID");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_GameID",
                table: "Turns",
                column: "GameID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Moves");

            migrationBuilder.DropTable(
                name: "Turns");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
