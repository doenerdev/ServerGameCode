using System;
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

        private ServerCode _server;
        private ServerClientShare.Helper.RandomGenerator _rndGenerator;
        private ServerClientShare.Helper.Die _die;
        private HexCellService _hexCellService;
        private HexMapService _hexMapService;
        private DeckService _deckService;
        private NetworkMessageService _networkMessageService;
        private GameRoomService _gameRoomService;

        public ServerClientShare.Helper.RandomGenerator RandomGenerator
        {
            get { return _rndGenerator;
            } 
        }
        public ServerClientShare.Helper.Die Die
        {
            get { return _die; }
        }
        public GameRoomService GameRoomService
        {
            get { return _gameRoomService; }
        }
        public HexCellService HexCellService
        {
            get { return _hexCellService; }
        }
        public HexMapService HexMapService
        {
            get { return _hexMapService; }
        }
        public DeckService DeckService
        {
            get { return _deckService; }
        }
        public NetworkMessageService NetworkMessageService
        {
            get { return _networkMessageService; }
        }

        public ServerCode Server { get { return _server; } }

        public ServiceContainer(ServerCode server, string roomId, RoomData roomData)
        {
            _server = server;
            _rndGenerator = new ServerClientShare.Helper.RandomGenerator();
            _die = new ServerClientShare.Helper.Die(_rndGenerator);
            _hexCellService = new HexCellService(_die, _rndGenerator);
            _hexMapService = new HexMapService(_hexCellService, HexMapSize.M);
            _deckService = new DeckService(_rndGenerator);
            _networkMessageService = new NetworkMessageService(Server);
            _gameRoomService = new GameRoomService(Server, roomId, roomData);
        }
    }
}
