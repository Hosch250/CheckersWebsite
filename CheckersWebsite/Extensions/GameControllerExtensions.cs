using System;
using System.Collections.Generic;
using System.Linq;
using CheckersWebsite.Enums;
using CheckersWebsite.Facade;

namespace CheckersWebsite.Extensions
{
    public static class GameControllerExtensions
    {
        public static int GetCurrentPosition(this GameController controller)
        {
            Checkers.GameVariant.PdnMembers pdnMembers;
            switch (controller.Variant)
            {
                case Variant.AmericanCheckers:
                    pdnMembers = Checkers.GameVariant.PdnMembers.AmericanCheckers;
                    break;
                case Variant.PoolCheckers:
                    pdnMembers = Checkers.GameVariant.PdnMembers.PoolCheckers;
                    break;
                default:
                    throw new ArgumentException("Variant Not Implemented");
            }

            return controller.CurrentCoord == null ? -1 : Checkers.PublicAPI.pdnBoard(pdnMembers)[controller.CurrentCoord.Row, controller.CurrentCoord.Column].Value;
        }

        public static Database.Game ToGame(this GameController controller)
        {
            var game = new Database.Game()
            {
                ID = controller.ID == Guid.Empty ? Guid.NewGuid() : controller.ID,
                CurrentPlayer = (int)controller.CurrentPlayer,
                GameStatus = (int) (controller.GameStatus == Status.InProgress ? controller.GetGameStatus() : controller.GameStatus),
                Fen = controller.Fen,
                InitialPosition = controller.InitialPosition,
                CurrentPosition = controller.GetCurrentPosition(),
                Variant = (int)controller.Variant,
                Turns = controller.MoveHistory.Select(s => s.ToPdnTurn()).ToList()
            };

            return game;
        }

        public static GameController ToGame(this Database.Game game)
        {
            var controller = GameController.FromPosition((Variant)game.Variant, game.Fen);
            controller.MoveHistory = game.Turns?.Select(s => s.ToPdnTurn()).ToList() ?? new List<PdnTurn>();

            if (game.CurrentPosition != -1)
            {
                Checkers.GameVariant.PdnMembers pdnMembers;
                switch ((Variant)game.Variant)
                {
                    case Variant.AmericanCheckers:
                        pdnMembers = Checkers.GameVariant.PdnMembers.AmericanCheckers;
                        break;
                    case Variant.PoolCheckers:
                        pdnMembers = Checkers.GameVariant.PdnMembers.PoolCheckers;
                        break;
                    default:
                        throw new ArgumentException("Variant Not Implemented");
                }

                controller.CurrentCoord = Checkers.PublicAPI.pdnBoardCoords(pdnMembers)[game.CurrentPosition];
            }

            controller.ID = game.ID;
            controller.GameStatus = (Status) game.GameStatus;
            
            return controller;
        }
    }
}
