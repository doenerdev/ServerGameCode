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
    public class GameRoomService : DatabaseInteraction<GameRoomService>
    {
        public const int TesingRoomSize = 1; //TODO REMOVE LATER

        private RandomGenerator _rndGenerator;
        private Die _die;
        private PlayerService _playerService;
        private ServerCode _server;
        private MatchDTO _matchDto;

        private string _currentPlayerIndex;

        private PlayerActionsLog _actionLog;
        private int _requiredRoomSize = TesingRoomSize; //TODO change back to 2
        private DatabaseObject _databaseEntry;

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
        public int RequiredRoomSize =>  _requiredRoomSize;
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

            _rndGenerator = new RandomGenerator();
            _die = new Die(_rndGenerator);
            _playerService = playerService;
        }

        public GameRoomService(DatabaseObject dbObject, ServerCode server, string gameId, PlayerService playerService, RoomData roomData = null) : this(server, gameId, playerService, roomData)
        {
            _matchDto = MatchDTO.FromDBObject(dbObject.GetObject("Match"));
            _actionLog = PlayerActionsLog.FromDBObject(dbObject.GetObject("PlayerActionLog"), server);
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

        public void SetActivePlayerId(string activePlayerName)
        {
            if (_matchDto?.Players?.Count(p => p.PlayerName == activePlayerName) > 0)
            {
                _currentPlayerIndex = activePlayerName;
                Console.WriteLine("Set next active player id:" + _currentPlayerIndex);
            }
            else
            {
                Console.WriteLine("Tried setting next active player id but the id wasn't present in the list");
            }
        }

        public void AddPlayer(Player player)
        {
            var leaderType = (LeaderType) Enum.Parse(typeof(LeaderType), player.JoinData["LeaderType"]);
            var playerDto = _playerService.GenerateInitialPlayer(player.ConnectUserId, _matchDto.Players.Count, leaderType);
            _matchDto.AddPlayer(playerDto);
        }

        public void RemovePlayer(Player player)
        {
            _matchDto.RemovePlayer(player.ConnectUserId);
        }

        public override DatabaseObject ToDBObject()
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

            DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
            dbGameplayPersistenceData.Set("Match", _matchDto.ToDBObject());
            dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
            dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
            dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
            dbGameplayPersistenceData.Set("PlayerActionLog", _actionLog.ToDBObject());
            dbObject.Set("Turns", new DatabaseArray()
            {
                dbGameplayPersistenceData
            });

            return dbObject;
        }

        public override void WriteToDb(BigDB dbClient)
        {
            dbClient.DeleteRange("GameSessions", "ByRequiredRoomSize", null, 0, 3, () =>
            {
                DatabaseObject dbObject = ToDBObject();
                dbClient.CreateObject(
                    "GameSessions",
                    _server.RoomId,
                    dbObject,
                    successCallback: receivedDbObject =>
                    {
                        _databaseEntry = receivedDbObject;
                        Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                    }
                );
            });

      
        }

        public void WriteActivePlayerToDb(BigDB dbClient)
        {
            dbClient.LoadOrCreate(
                "GameSessions",
                _server.RoomId,
                successCallback: receivedDbObject =>
                {
                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    _databaseEntry = receivedDbObject;
                    _databaseEntry.Set("CurrentPlayerName", CurrentPlayer.PlayerName);
                    _databaseEntry.Save();
                    Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("An error occured while trying to write Active Player to DB");
                }
            );
        }

        public void UpdateMatch(MatchDTO dto)
        {
            _matchDto = dto;
        }

        public void GenerateInitialGameplayDataDTO(Player player, Action<InitialGameplayDataDTO> successCallback)
        {
            var playerDto = MatchDTO.Players.SingleOrDefault(p => p.PlayerName == player.ConnectUserId);
            Console.WriteLine(playerDto);
            if (playerDto == null) return;

            if (MatchDTO.TurnNumber == 0)
            {
                InitialGameplayDataDTO dto = new InitialGameplayDataDTO()
                {
                    Match = MatchDTO,
                    HexMap = _server.ServiceContainer.HexMapService.CurrentHexMapDto,
                    Marketplace = _server.ServiceContainer.DeckService.Marketplace,
                    Deck = _server.ServiceContainer.DeckService.Deck,
                    ActionLog = _actionLog.DTO
                };
                successCallback(dto);
            }
            else
            {
                _server.PlayerIO.BigDB.Load(
                    "GameSessions",
                    _server.RoomId,
                    successCallback: receivedDbObject =>
                    {
                        DatabaseArray turns = receivedDbObject.GetArray("Turns");

                        InitialGameplayDataDTO dto;
                        if (playerDto.CurrentTurn < MatchDTO.TurnNumber)
                        {
                            dto = InitialGameplayDataDTO.FromDBObject(turns.GetObject(playerDto.CurrentTurn > 0 ? playerDto.CurrentTurn - 1 : 0));
                        }
                        else
                        {
                            dto = InitialGameplayDataDTO.FromDBObject(turns.GetObject(playerDto.CurrentTurn));
                        }

                        Console.WriteLine("Retrieved Initial Gameplay Data for Turn:" + (playerDto.CurrentTurn > 0 ? playerDto.CurrentTurn - 1 : 0));
                        successCallback(dto);
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Active Player to DB");
                    }
                );
            }
            
        }
    }
}
