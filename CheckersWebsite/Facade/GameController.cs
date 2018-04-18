using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.FSharp.Core;

namespace CheckersWebsite.Facade
{
    public class GameController : INotifyPropertyChanged
    {
        internal GameController(Variant variant, Board board, Player currentPlayer, string initialPosition, List<PdnTurn> moveHistory, Coord currentCoord = null)
        {
            Variant = variant;
            Board = board;
            CurrentPlayer = currentPlayer;
            InitialPosition = initialPosition;
            MoveHistory = moveHistory;
            CurrentCoord = currentCoord;

            Fen = Checkers.PublicAPI.createFen(variant.ToGameVariant().pdnMembers, CurrentPlayer.ConvertBack(), Board);
        }

        public GameController(Variant variant, Board board, Player currentPlayer)
            : this(variant, board, currentPlayer, Checkers.PublicAPI.createFen(variant.ToGameVariant().pdnMembers, currentPlayer.ConvertBack(), board), new List<PdnTurn>()) { }

        public GameController(Checkers.GameController.GameController gameController)
            : this(gameController.Variant.ToVariant(), gameController.Board, gameController.CurrentPlayer.Convert(), gameController.InitialPosition, gameController.MoveHistory.Select(item => (PdnTurn)item).ToList(), gameController.CurrentCoord) { }

        public GameController WithBoard(string fen) =>
            new GameController(Variant, FromPosition(Variant, fen).Board, CurrentPlayer, InitialPosition, MoveHistory, CurrentCoord);
        
        public static GameController FromPosition(Variant variant, string fenPosition)
        {
            try
            {
                return Checkers.PublicAPI.controllerFromFen(variant.ToGameVariant(), fenPosition);
            }
            catch
            {
                // invalid fen entered
                // todo: notify the user
                return null;
            }
        }

        public static GameController FromVariant(Variant variant)
        {
            switch (variant)
            {
                case Variant.AmericanCheckers:
                    return Checkers.GameController.GameController.newAmericanCheckersGame;
                case Variant.PoolCheckers:
                    return Checkers.GameController.GameController.newPoolCheckersGame;
                default:
                    throw new ArgumentException(nameof(variant));
            }
        }

        public static bool TryFromPosition(Variant variant, string fenPosition, out GameController controller)
        {
            try
            {
                controller = Checkers.PublicAPI.controllerFromFen(variant.ToGameVariant(), fenPosition);
                return true;
            }
            catch
            {
                controller = FromVariant(variant);
                return false;
            }
        }

        public Variant Variant { get; internal set; }
        public Player CurrentPlayer { get; internal set; }
        public Coord CurrentCoord { get; internal set; }
        public string InitialPosition { get; internal set; }
        public string Fen { get; internal set; }
        public Guid ID { get; internal set; }

        private List<PdnTurn> _moveHistory;
        public List<PdnTurn> MoveHistory
        {
            get { return _moveHistory; }
            set
            {
                _moveHistory = value;
                OnPropertyChanged();
            }
        }

        private Board _board;
        public Board Board
        {
            get { return _board; }
            set
            {
                _board = value;
                OnPropertyChanged();
            }
        }

        public GameController Move(Coord startCoord, Coord endCoord) =>
            Checkers.PublicAPI.movePiece(startCoord, endCoord, this);

        public GameController Move(IEnumerable<Coord> moves) =>
            Checkers.PublicAPI.move(moves.Select(item => (Checkers.Generic.Coord)item), this);

        public List<List<Coord>> GetValidMoves()
        {
            IsDrawn();
            var moves = Checkers.PublicAPI.getValidMoves(this);
            return moves.Select(i => i.Select(c => (Coord)c).ToList()).ToList();
        }

        public Status GetGameStatus()
        {
            var gameStatus = Status.InProgress;
            if (IsDrawn())
            {
                gameStatus = Status.Drawn;
            }
            if (IsWon())
            {
                gameStatus = GetWinningPlayer() == Player.Black ? Status.BlackWin : Status.WhiteWin;
            }

            return gameStatus;
        }

        public Status GameStatus { get; set; }

        public bool IsValidMove(Coord startCoord, Coord endCoord) =>
            Checkers.PublicAPI.isValidMove(startCoord, endCoord, this);

        public IEnumerable<Coord> GetMove(int searchDepth, CancellationToken token) =>
            Checkers.PublicAPI.getMove(searchDepth, this, token).Select(coord => (Coord)coord);

        public GameController TakebackMove() =>
            Checkers.PublicAPI.takeBackMove(this);

        public bool IsDrawn()
            => Checkers.PublicAPI.isDrawn(this);

        public Player? GetWinningPlayer()
        {
            var player = Checkers.PublicAPI.winningPlayer(this);
            return Equals(player, FSharpOption<Checkers.Generic.Player>.None) ? new Player?() : player.Value.Convert();
        }

        public bool IsWon() =>
            Checkers.PublicAPI.isWon(this);

        public static implicit operator GameController(Checkers.GameController.GameController controller)
        {
            return new GameController(controller);
        }

        public static implicit operator Checkers.GameController.GameController(GameController controller)
        {
            var moveHistory = Checkers.Generic.listFromSeq(controller.MoveHistory.Select(item => (Checkers.Generic.PdnTurn)item)).Value;

            return new Checkers.GameController.GameController(controller.Variant.ToGameVariant(), controller.Board, controller.CurrentPlayer.ConvertBack(), controller.InitialPosition, moveHistory, controller.CurrentCoord);
        }

        public static implicit operator GameController(FSharpOption<Checkers.GameController.GameController> controller)
        {
            return Equals(controller, FSharpOption<Checkers.GameController.GameController>.None)
                ? null
                : new GameController(controller.Value);
        }

        public static implicit operator FSharpOption<Checkers.GameController.GameController>(GameController controller)
        {
            var moveHistory = Checkers.Generic.listFromSeq(controller.MoveHistory.Select(item => (Checkers.Generic.PdnTurn)item)).Value;

            return FSharpOption<Checkers.GameController.GameController>.Some(
                    new Checkers.GameController.GameController(controller.Variant.ToGameVariant(), controller.Board, controller.CurrentPlayer.ConvertBack(), controller.InitialPosition, moveHistory, controller.CurrentCoord));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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
