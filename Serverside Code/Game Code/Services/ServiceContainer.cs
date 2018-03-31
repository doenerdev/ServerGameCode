using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerGameCode.Helper;
using ServerClientShare.Services;
using ServerClientShare.Helper;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class ServiceContainer : IServerAspect
    {

        private readonly ServerCode _server;
        private readonly ServerClientShare.Helper.RandomGenerator _rndGenerator;
        private readonly ServerClientShare.Helper.Die _die;
        private readonly HexCellService _hexCellService;
        private readonly HexMapService _hexMapService;
        private readonly DeckService _deckService;
        private readonly NetworkMessageService _networkMessageService;
        private readonly GameRoomService _gameRoomService;
        private readonly ResourceService _resourceService;
        private readonly PlayerService _playerService;
        private readonly DatabaseService _databaseService;
        private readonly PersistenceService _persistenceService;
        private readonly LeaderService _leaderService;

        public ServerClientShare.Helper.RandomGenerator RandomGenerator => _rndGenerator;
        public ServerClientShare.Helper.Die Die => _die;
        public GameRoomService GameRoomService => _gameRoomService;
        public HexCellService HexCellService => _hexCellService;
        public HexMapService HexMapService => _hexMapService;
        public DeckService DeckService => _deckService;
        public NetworkMessageService NetworkMessageService => _networkMessageService;
        public ResourceService ResourceService => _resourceService;
        public PlayerService PlayerService => _playerService;
        public DatabaseService DatabaseService => _databaseService;
        public PersistenceService PersistenceService => _persistenceService;
        public LeaderService LeaderService => _leaderService;
        public ServerCode Server { get { return _server; } }

        public ServiceContainer(ServerCode server, string roomId, RoomData roomData)
        {
            _server = server;
            _databaseService = new DatabaseService(server);
            _rndGenerator = new ServerClientShare.Helper.RandomGenerator();
            _die = new ServerClientShare.Helper.Die(_rndGenerator);
            _hexCellService = new HexCellService(_die, _rndGenerator);
            _networkMessageService = new NetworkMessageService(Server);
            _resourceService = new ResourceService(_die, _rndGenerator);
            _leaderService = new LeaderService();
            _playerService = new PlayerService(_resourceService, _leaderService);
            _deckService = new DeckService(_rndGenerator);
            _gameRoomService = new GameRoomService(Server, roomId, _playerService, roomData);
            _hexMapService = new HexMapService(_hexCellService, _gameRoomService.RequiredRoomSize);
            _persistenceService = new PersistenceService(server, _gameRoomService, _hexMapService, _deckService, _playerService);
        }

        public ServiceContainer(DatabaseObject dbObject, ServerCode server, string roomId, RoomData roomData)
        {
            DatabaseArray turns = dbObject.GetArray("Turns");
            var currentTurnDb = turns.GetObject(turns.Count-1);

            _server = server;
            _databaseService = new DatabaseService(server);
            _rndGenerator = new ServerClientShare.Helper.RandomGenerator();
            _die = new ServerClientShare.Helper.Die(_rndGenerator);
            _hexCellService = new HexCellService(_die, _rndGenerator);
            _deckService = new DeckService(currentTurnDb, _rndGenerator);
            _networkMessageService = new NetworkMessageService(Server);
            _resourceService = new ResourceService(_die, _rndGenerator);
            _leaderService = new LeaderService();
            _playerService = new PlayerService(_resourceService, _leaderService);
            Console.WriteLine("GameRoomSrvice");
            _gameRoomService = new GameRoomService(currentTurnDb.GetObject("Match"), dbObject.GetObject("PlayerActionLog"), Server, roomId, _playerService, roomData);
            _hexMapService = new HexMapService(currentTurnDb, _hexCellService, _gameRoomService.RequiredRoomSize);
            _persistenceService = new PersistenceService(dbObject, server, _gameRoomService, _hexMapService, _deckService, _playerService);
        }
    }
}
