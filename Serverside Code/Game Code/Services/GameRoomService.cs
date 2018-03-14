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
        }

        public void RemovePlayer(Player player)
        {
            _matchDto.RemovePlayer(player.ConnectUserId);
        }

        private DatabaseObject GenerateInitialDBObject()
        {
            DatabaseObject dbObject = new DatabaseObject();
            dbObject.Set("GameId", _server.RoomId);
            dbObject.Set("GameStartedState", GameStartedState.ToString("G"));
            dbObject.Set("CurrentPlayerName", CurrentPlayer.PlayerName);
            dbObject.Set("RequiredRoomSize", _requiredRoomSize);

            DatabaseArray dbPlayerIds = new DatabaseArray();
            foreach (var playerId in _matchDto.Players.Select(p => p.PlayerName))
            {
                dbPlayerIds.Add(playerId);
            }
            dbObject.Set("PlayerIds", dbPlayerIds);
            dbObject.Set("PlayerActionLog", _actionLog.ToDBObject());

            DatabaseObject initialPersistenceData = new DatabaseObject();
            initialPersistenceData.Set("Match", _matchDto.ToDBObject());
            initialPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
            initialPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
            initialPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
            dbObject.Set("InitialTurns", new DatabaseArray()
            {
                initialPersistenceData
            });

            DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
            dbGameplayPersistenceData.Set("Match", _matchDto.ToDBObject());
            dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
            dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
            dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
            dbObject.Set("Turns", new DatabaseArray()
            {
                dbGameplayPersistenceData
            });

            return dbObject;
        }

        public void WriteInitialDataToDb(BigDB dbClient, Action successCallback)
        {
            dbClient.DeleteRange("GameSessions", "ByRequiredRoomSize", null, 0, 3, () =>
            {
                DatabaseObject dbObject = GenerateInitialDBObject();
                dbClient.CreateObject(
                    "GameSessions",
                    _server.RoomId,
                    dbObject,
                    successCallback: receivedDbObject =>
                    {
                        successCallback();
                        Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                    }
                );
            });
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

        public void GenerateInitialGameplayDataDTO(Player player, Action<InitialGameplayDataDTO> successCallback)
        {
            var playerDto = MatchDTO.Players.SingleOrDefault(p => p.PlayerName == player.ConnectUserId);
            Console.WriteLine(playerDto);
            if (playerDto == null) return;

            _server.PlayerIO.BigDB.Load(
                "GameSessions",
                _server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (MatchDTO.TurnNumber == 0)
                    {
                        _server.IsNewGame(newGame =>
                        {
                            InitialGameplayDataDTO dto;
                            if (newGame)
                            {
                                Console.WriteLine("INITIAL: IS NEW GAME");
                                dto = new InitialGameplayDataDTO()
                                {
                                    Match = MatchDTO,
                                    HexMap = _server.ServiceContainer.HexMapService.CurrentHexMapDto,
                                    Marketplace = _server.ServiceContainer.DeckService.Marketplace,
                                    Deck = _server.ServiceContainer.DeckService.Deck,
                                    ActionLog = _actionLog.DTO
                                };
                            }
                            else
                            {
                                Console.WriteLine("INITIAL: IS CONTINUED GAME");
                                dto = InitialGameplayDataDTO.FromDBObject(receivedDbObject.GetObject("InitialData"));
                                dto.ActionLog = PlayerActionsLogDTO.FromDBObject(receivedDbObject.GetObject("PlayerActionLog"));
                                Console.WriteLine("Retrieved Initial Gameplay Data for Turn:" + (playerDto.CurrentTurn > 0 ? playerDto.CurrentTurn - 1 : 0));
                            }
                            successCallback(dto);
                        });
                    }
                    else
                    {
                        InitialGameplayDataDTO dto;
                        DatabaseArray turns = receivedDbObject.GetArray("Turns");

                        if (playerDto.CurrentTurn < MatchDTO.TurnNumber)
                        {
                            dto = InitialGameplayDataDTO.FromDBObject(turns.GetObject(playerDto.CurrentTurn > 0 ? playerDto.CurrentTurn - 1 : 0));
                        }
                        else
                        {
                            dto = InitialGameplayDataDTO.FromDBObject(turns.GetObject(playerDto.CurrentTurn));
                        }
                        dto.ActionLog = PlayerActionsLogDTO.FromDBObject(receivedDbObject.GetObject("PlayerActionLog"));

                        Console.WriteLine("Retrieved Initial Gameplay Data for Turn:" + (playerDto.CurrentTurn > 0 ? playerDto.CurrentTurn - 1 : 0));
                        successCallback(dto);
                    }
                },
                errorCallback: error =>
                {
                    Console.WriteLine("An error occured while trying to write Active Player to DB");
                }
            );
        }
    }
}
