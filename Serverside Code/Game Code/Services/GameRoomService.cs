using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Helper;
using ServerGameCode.Helper;

namespace ServerGameCode
{
    public class GameRoomService : DatabaseInteraction<GameRoomService>
    {
        public const int TesingRoomSize = 1; //TODO REMOVE LATER

        private RandomGenerator _rndGenerator;
        private Die _die;
        private Game<Player> _game;
        private MatchDTO _matchDto;

        private string _currentPlayerIndex;

        private PlayerCommandLog _commands;
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
        public IEnumerable<Player> Players => _game?.Players;
        public int RequiredRoomSize =>  _requiredRoomSize;
        public PlayerCommandLog PlayerCommands => _commands;
        

        public GameRoomService(Game<Player> game, string gameId, RoomData roomData = null)
        {
            _game = game;
            _matchDto = new MatchDTO()
            {
                GameId = gameId,
            };
            _commands = new PlayerCommandLog();
            InitializeFromRoomData(roomData);

            _rndGenerator = new RandomGenerator();
            _die = new Die(_rndGenerator);
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
            _matchDto.AddPlayer(player.ConnectUserId);
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

        public override DatabaseObject ToDdObject()
        {
            DatabaseObject dbObject = new DatabaseObject();
            dbObject.Set("GameId", _game.RoomId);
            dbObject.Set("GameStartedState", GameStartedState.ToString("G"));
            dbObject.Set("CurrentPlayerName", CurrentPlayer.PlayerName);
            dbObject.Set("RequiredRoomSize", _requiredRoomSize);

            DatabaseArray commandLogDb = new DatabaseArray();
            if (_commands.PlayerCommands != null)
            {
                foreach (var command in _commands.PlayerCommands)
                {
                    commandLogDb.Add(command.ToString());
                }
            }
            dbObject.Set("CommandLog", commandLogDb);

            DatabaseArray dbPlayerIds = new DatabaseArray();
            foreach (var playerId in _matchDto.Players.Select(p => p.PlayerName))
            {
                dbPlayerIds.Add(playerId);
            }
            dbObject.Set("PlayerIds", dbPlayerIds);

            return dbObject;
        }

        public override void WriteToDb(BigDB dbClient)
        {
            DatabaseObject dbObject = ToDdObject();
            dbClient.CreateObject(
                "GameSessions",
                _game.RoomId,
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
        }

        public void WriteActivePlayerToDb(BigDB dbClient)
        {
            dbClient.LoadOrCreate(
                "GameSessions",
                _game.RoomId,
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

        public void WriteCommandLogToDb(BigDB dbClient)
        {
            dbClient.LoadOrCreate(
                "GameSessions",
                _game.RoomId,
                successCallback: receivedDbObject =>
                {
                    _databaseEntry = receivedDbObject;

                    DatabaseArray commandLogDb = new DatabaseArray();
                    foreach (var commandList in _commands.PlayerCommands)
                    {
                        DatabaseArray commandLogTurnX = new DatabaseArray();
                        foreach (var command in commandList.Value)
                        {
                            commandLogTurnX.Add(command.ToDbArray());
                        }
                        commandLogDb.Add(commandLogTurnX);
                    }
                    _databaseEntry.Set("CommandLog", commandLogDb);
                    _databaseEntry.Save();
                    Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("An error occured while trying to write Command Log to DB");
                }
            );
        }
    }
}
