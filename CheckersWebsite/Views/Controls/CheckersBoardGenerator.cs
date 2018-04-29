using CheckersWebsite.Facade;
using System;
using System.Collections.Generic;
using System.IO;

namespace CheckersWebsite.Views.Controls
{
    public static class ComponentGenerator
    {
        public static string GetBoard(GameController game, Dictionary<string, object> viewData)
        {
            using (var stringWriter = new StringWriter())
            {
                Action<string> write = stringWriter.Write;

                var playerID = (Guid)viewData["playerID"];
                var blackPlayerID = (Guid)viewData["blackPlayerID"];
                var whitePlayerID = (Guid)viewData["whitePlayerID"];
                var orientation = (Player)viewData["orientation"];
                var theme = (Theme)viewData["theme"];

                var isCurrentPlayer = playerID == blackPlayerID && game.CurrentPlayer == Player.Black || playerID == whitePlayerID && game.CurrentPlayer == Player.White;
                write($@"<div class=""board {game.CurrentPlayer} {(isCurrentPlayer ? "current-player" : "")}"" id=""{game.ID}"" orientation=""{orientation}"">");

                for (var row = 0; row < 8; row++)
                {
                    for (var col = 0; col < 8; col++)
                    {
                        write($@"<img class=""square {(GetAdjustedIndex(col) % 2 == GetAdjustedIndex(row) % 2 ? "" : "drop-target")}""" +
                                  $@"id=""square{GetAdjustedIndex(row)}{GetAdjustedIndex(col)}""" +
                                  $@"src=""/images/{theme}Theme/{(GetAdjustedIndex(col) % 2 == GetAdjustedIndex(row) % 2 ? "Light" : "Dark")}{theme}.png""" +
                                  $@"style=""grid-row: {GetAdjustedIndex(row) + 1}; grid-column: {GetAdjustedIndex(col) + 1}"" />");

                        var piece = game.Board[row, col];
                        if (piece != null)
                        {
                            write($@"<img id=""piece{GetAdjustedIndex(row)}{GetAdjustedIndex(col)}""" +
                                      $@"class=""piece""" +
                                      $@"player=""{piece.Player}""" +
                                      $@"src=""/images/{theme}Theme/{piece.Player}{piece.PieceType}.png""" +
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
    }
}
