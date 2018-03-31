using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerClientShare.Helper;
using ServerClientShare.Services;
using ServerGameCode.Helper;
using ServerClientShare.Models;

namespace ServerGameCode
{
    public class GameRoomService
    {
        public const int TesingRoomSize = 2; //TODO REMOVE LATER
        private PlayerService _playerService;
        private ServerCode _server;
        private MatchDTO _matchDto;

        private PlayerActionsLog _actionLog;
        private int _requiredRoomSize = TesingRoomSize; //TODO change back to 2

        public const int MaxRoomSize = 4;
        public const int MinRoomSize = TesingRoomSize; //TODO change back to 2
        public const int DefaultRoomSize = TesingRoomSize; //TODO change back to 2

        public GameStartedState GameStartedState { get; set; }

        public MatchDTO MatchDTO => _matchDto;
        public PlayerDTO CurrentPlayer
        {
            get => _matchDto?.CurrentPlayerDto;
            set => _matchDto.CurrentPlayerIndex = _matchDto.Players.TakeWhile(p => p.PlayerName != value.PlayerName).Count();
        }
        public IEnumerable<Player> Players => _server?.Players;
        public int RequiredRoomSize => _requiredRoomSize;
        public PlayerActionsLog PlayerActionLog => _actionLog;


        public GameRoomService(ServerCode server, string gameId, PlayerService playerService, RoomData roomData = null)
        {
            _server = server;
            _matchDto = new MatchDTO()
            {
                GameId = gameId,
            };
            _actionLog = new PlayerActionsLog(_server);
            InitializeFromRoomData(roomData);
            _playerService = playerService;
        }

        public GameRoomService(DatabaseObject dbObjectMatch, DatabaseObject dbObjectActionLog, ServerCode server,
            string gameId, PlayerService playerService, RoomData roomData = null) : this(server, gameId, playerService, roomData)
        {
            _matchDto = MatchDTO.FromDBObject(dbObjectMatch);
            _actionLog = PlayerActionsLog.FromDBObject(dbObjectActionLog, server);
        }

        private void InitializeFromRoomData(RoomData roomData)
        {
            if (roomData != null && roomData.ContainsKey("RequiredRoomSize") &&
                Int32.TryParse(roomData["RequiredRoomSize"], out int roomSize))
            {
                _requiredRoomSize = roomSize >= MinRoomSize && roomSize <= MaxRoomSize ? roomSize : DefaultRoomSize;
            }
            else
            {
                _requiredRoomSize = DefaultRoomSize;
            }
        }

        public void AddPlayer(Player player)
        {
            var leaderType = (LeaderType)Enum.Parse(typeof(LeaderType), player.JoinData["LeaderType"]);
            var playerDto = _playerService.GenerateInitialPlayer(player.ConnectUserId, _matchDto.Players.Count, leaderType);
            _matchDto.AddPlayer(playerDto);
            //_server.ServiceContainer.PersistenceService.UpdateGameSessionBaseData();
            Console.WriteLine("ADDED PLAYER TO DTO");
        }

        public void UpdateMatch(Player playerSender, MatchDTO dto)
        {
            //keep the old playerDtos ActionLogIndex and TurnNumber, it should only be updated through messages
            foreach (var playerDto in _matchDto.Players)
            {
                var player = dto.Players.SingleOrDefault(p => p.PlayerName == playerDto.PlayerName);
                if (player != null)
                {
                    player.CurrentActionLogIndex = playerDto.CurrentActionLogIndex;
                    player.CurrentTurn = playerDto.CurrentTurn;

                    dto.RemovePlayer(playerDto.PlayerName);
                    dto.AddPlayer(player);
                }
            }
            _matchDto = dto;
        }

        public void UpdateActionLogIndex(Player player, int index)
        {
            var playerDto = MatchDTO.Players.SingleOrDefault(p => p.PlayerName == player.ConnectUserId);
            if (playerDto != null)
            {
                playerDto.CurrentActionLogIndex = index;
            }
        }

        public void UpdatePlayerTurnNumber(Player player, int turnNumber)
        {
            var playerDto = MatchDTO.Players.SingleOrDefault(p => p.PlayerName == player.ConnectUserId);
            if (playerDto != null)
            {
                playerDto.CurrentTurn = turnNumber;
            }
        }

    }
}
