using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Helper;
using ServerClientShare.Services;
using ServerGameCode.Helper;

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
            var playerDto = _playerService.GenerateInitialPlayer(player.ConnectUserId, _matchDto.Players.Count);
            _matchDto.AddPlayer(playerDto);
        }

        public void RemovePlayer(Player player)
        {
            _matchDto.RemovePlayer(player.ConnectUserId);
        }

        public new static GameRoomService CreateFromDbObject(Game<Player> game, DatabaseObject dbObject)
        {
            try
            {
                /*string gameId = dbObject.GetString("GameId");
                GameRoomService gameRoomfService = new GameRoomService(server, gameId);
                gameRoomfService.GameStartedState = (GameStartedState)Enum.Parse(typeof(GameStartedState), dbObject.GetString("GameStartedState"));
                gameRoomfService.CurrentPlayer = dbObject.GetString("CurrentPlayerName");
                gameRoomfService._requiredRoomSize = Int32.Parse(dbObject.GetString("RequiredRoomSize"));

                DatabaseArray playerIds = dbObject.GetArray("PlayerIds");
                foreach (var playerId in playerIds)
                {
                    gameRoomfService._playerIds.Add(playerId.ToString());
                }
                Console.WriteLine("Successfully parsed db object");
                return gameRoomfService;*/
                return null;
            }
            catch
            {
                Console.WriteLine("Error while parsing db object. Might be corrupted");
                return null;
            }
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

            dbObject.Set("Match", _matchDto.ToDBObject());
            dbObject.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
            dbObject.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
            dbObject.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
            dbObject.Set("ActionLog", _actionLog.ToDBObject());

            return dbObject;
        }

        public override void WriteToDb(BigDB dbClient)
        {
            dbClient.DeleteRange("GameSessions", "ByRequiredRoomSize", null, 0, 3);

            DatabaseObject dbObject = ToDBObject();
            dbClient.CreateObject(
                "GameSessions",
                _server.RoomId,
                dbObject,
                successCallback: receivedDbObject =>
                {
                    _databaseEntry = receivedDbObject;
                    Console.WriteLine("Sucessfully wrote Server Room Info to DB");

                    dbClient.Load("GameSessions", _server.RoomId, (DatabaseObject result) =>
                    {
                        HexMapDTO dto = HexMapDTO.FromDBObject(result.GetObject("HexMap"));
                    });
                },
                errorCallback: error =>
                {
                    Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                }
            );
        }

        public void WriteActivePlayerToDb(BigDB dbClient)
        {
            dbClient.LoadOrCreate(
                "GameSessions",
                _server.RoomId,
                successCallback: receivedDbObject =>
                {
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
    }
}
