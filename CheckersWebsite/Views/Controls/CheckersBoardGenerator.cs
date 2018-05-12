using CheckersWebsite.Enums;
using CheckersWebsite.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace CheckersWebsite.Views.Controls
{
    public static class ComponentGenerator
    {
        public static string GetBoard(GameViewModel game, Dictionary<string, object> viewData)
        {
            using (var stringWriter = new StringWriter())
            {
                Action<string> write = stringWriter.Write;

                var playerID = (Guid)viewData["playerID"];
                var orientation = (Player)viewData["orientation"];

                var isCurrentPlayer = game.DisplayingLastMove && playerID == game.BlackPlayerID && game.CurrentPlayer == Player.Black || playerID == game.WhitePlayerID && game.CurrentPlayer == Player.White;
                write($@"<div class=""board {game.CurrentPlayer} {(isCurrentPlayer ? "current-player" : "")}"" id=""{game.ID}"" orientation=""{orientation}"">");

                for (var row = 0; row < 8; row++)
                {
                    for (var col = 0; col < 8; col++)
                    {
                        var piece = game.Board[row, col];

                        write($@"<img class=""square {(GetAdjustedIndex(col) % 2 == GetAdjustedIndex(row) % 2 ? "" : "drop-target")}""" +
                                  (piece != null || GetAdjustedIndex(col) % 2 == GetAdjustedIndex(row) % 2 ? "" : @"tabindex=""0""") +
                                  $@"title=""square on row {row} column {col}""" +
                                  $@"id=""square{GetAdjustedIndex(row)}{GetAdjustedIndex(col)}""" +
                                  $@"src=""/images/[theme]Theme/{(GetAdjustedIndex(col) % 2 == GetAdjustedIndex(row) % 2 ? "Light" : "Dark")}[theme].png""" +
                                  $@"style=""grid-row: {GetAdjustedIndex(row) + 1}; grid-column: {GetAdjustedIndex(col) + 1}"" />");

                        if (piece != null)
                        {
                            write($@"<img id=""piece{GetAdjustedIndex(row)}{GetAdjustedIndex(col)}""" +
                                  $@"tabindex=""0""" +
                                  $@"title=""{piece.Player} {piece.PieceType} on row {row} column {col}""" +
                                  $@"class=""piece""" +
                                  $@"player=""{piece.Player}""" +
                                  $@"src=""/images/[theme]Theme/{piece.Player}{piece.PieceType}.png""" +
                                  $@"style=""grid-row: {GetAdjustedIndex(row) + 1}; grid-column: {GetAdjustedIndex(col) + 1}"" />");
                        }
                    }
                }

                write("</div>");

                stringWriter.Flush();
                return stringWriter.ToString();

                int GetAdjustedIndex(int value)
                {
                    return orientation == Player.White ? value : 7 - value;
                }
            }
        }

        public static string GetMoveControl(List<PdnTurnViewModel> turns)
        {
            using (var stringWriter = new StringWriter())
            {
                Action<string> write = stringWriter.Write;

                write($@"<ol class=""moves"">");

                foreach (var turn in turns)
                {
                    write("<li>");

                    write($@"<input id=""{turn.BlackMove?.ID}"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.BlackMove?.DisplayString}"" />");
                    write($@"<label for=""{turn.BlackMove?.ID}"" onclick=""displayGame('{turn.BlackMove?.ID}')"">{turn.BlackMove?.DisplayString}</label>");

                    if (turn.WhiteMove != null)
                    {
                        write($@"<input id=""{turn.WhiteMove?.ID}"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.WhiteMove?.DisplayString}"" />");
                        write($@"<label for=""{turn.WhiteMove?.ID}"" onclick=""displayGame('{turn.WhiteMove?.ID}')"">{turn.WhiteMove?.DisplayString}</label>");
                    }

                    write("</li>");
                }

                write("</ol>");

                stringWriter.Flush();
                return stringWriter.ToString();
            }
        }
    }
}
