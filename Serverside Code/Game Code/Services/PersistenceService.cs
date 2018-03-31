using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerClientShare.Models;
using ServerClientShare.PeristenceMessages;
using ServerClientShare.Services;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class PersistenceService : IServerAspect
    {
        private GameRoomService _gameRoomService;
        private HexMapService _hexMapService;
        private DeckService _deckService;
        private PlayerService _playerService;

        public ServerCode Server { get; private set; }

        public GameSessionsPersistenceDataDTO PersistenceData { get; private set; }

        public GameSessionMetaDataDTO MetaData
        {
            get
            {
                GameSessionMetaDataDTO metaData = new GameSessionMetaDataDTO();
                metaData.GameId = Server.RoomId;
                metaData.CurrentPlayerName = this.GameRoomService().MatchDTO.CurrentPlayerDto.PlayerName;
                metaData.GameStartedState = this.GameRoomService().GameStartedState;
                metaData.RequiredRoomSize = this.GameRoomService().RequiredRoomSize;

                foreach (var player in this.GameRoomService().MatchDTO.Players)
                {
                    metaData.Players.Add(new PlayerMetaDataDTO()
                    {
                        PlayerName = player.PlayerName,
                        PlayerIndex = player.PlayerIndex,
                        Score = player.Score,
                        IsOnline = true, //TODO needs to be added and updated to playerDTO 
                        Leader = new LeaderMetaDataDTO()
                        {
                            LeaderType = player.Leader.Type,
                            Name = player.Leader.Name
                        }
                    });
                }

                return metaData;
            }
        }

        public PersistenceService(ServerCode server, GameRoomService gameRoomService, HexMapService hexMapService, DeckService deckService, PlayerService playerService)
        {
            Server = server;
            _gameRoomService = gameRoomService;
            _hexMapService = hexMapService;
            _deckService = deckService;
            _playerService = playerService;
        }

        public PersistenceService(DatabaseObject dbObject, ServerCode server, GameRoomService gameRoomService, HexMapService hexMapService, DeckService deckService, PlayerService playerService) : this(server, gameRoomService, hexMapService, deckService, playerService)
        {
            PersistenceData = GameSessionsPersistenceDataDTO.FromDBObject(dbObject, Server);
            Console.WriteLine("Initialized Persitence Data From DTO:" +  PersistenceData.Turns.Count + " |Initial:" + PersistenceData.InitialTurns.Count);
        }

        public void Initialize(bool continuedGame = false)
        {
            if (PersistenceData != null) return;

            GameSessionsPersistenceDataDTO dataDto = new GameSessionsPersistenceDataDTO();
            dataDto.GameId = Server.RoomId;
            dataDto.CreatedTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; //unix timestamp
            dataDto.ActionLog = _gameRoomService.PlayerActionLog;

            var playerDtos = new List<PlayerDTO>();
            int i = 0;
            foreach (var player in _gameRoomService.MatchDTO.Players)
            {
                //TODO change from Server.Players to _gameRoomService.MatchDTO.Players => check if it is still working
                dataDto.PlayerIds.Add(player.PlayerName);
                var leaderType = player.Leader.Type;
                playerDtos.Add(_playerService.GenerateInitialPlayer(player.PlayerName, i, leaderType));
                i++;
            }
            if(continuedGame == false)
                _hexMapService.GenerateNewHexMap(playerDtos);
            
            dataDto.InitialTurns.Add(new GameSessionTurnDataDTO()
            {
                Match = _gameRoomService.MatchDTO,
                HexMap = _hexMapService.CurrentHexMapDto,
                Deck = _deckService.Deck,
                Marketplace = _deckService.Marketplace
            });

            dataDto.Turns.Add(new GameSessionTurnDataDTO()
            {
                Match = _gameRoomService.MatchDTO,
                HexMap = _hexMapService.CurrentHexMapDto,
                Deck = _deckService.Deck,
                Marketplace = _deckService.Marketplace
            });

            PersistenceData = dataDto;
        }

        public void IsNewGame(string gameId, Action<bool> callback)
        {
            this.DatabaseService().DoesGameMetaDataExist(
                gameId,
                callback: callback
            );
        }

        public void AddInitialTurnData(int turnNumber, int activePlayerIndex)
        {
            Console.WriteLine("Add Initial Turn for index:" + turnNumber);
            var match = this.GameRoomService().MatchDTO;
            match.GamePhase = GamePhase.DrawCards;
            match.CurrentPlayerIndex = activePlayerIndex;

            var data = new GameSessionTurnDataDTO()
            {
                Match = match,
                HexMap = _hexMapService.CurrentHexMapDto,
                Deck = _deckService.Deck,
                Marketplace = _deckService.Marketplace
            };
            Console.WriteLine("Intial Marketplace Phase:" + data.Match.GamePhase);

            if (PersistenceData.InitialTurns.Count > turnNumber)
            {
                PersistenceData.InitialTurns[turnNumber] = data;
            }
            else if(PersistenceData.InitialTurns.Count == turnNumber)
            {
                PersistenceData.InitialTurns.Add(data);
            }

            this.DatabaseService().WriteInitialTurnDataToDb(turnNumber);
        }

        private void AddTurnData(int turnNumber)
        {
            var data = new GameSessionTurnDataDTO()
            {
                Match = this.GameRoomService().MatchDTO,
                HexMap = this.HexMapService().CurrentHexMapDto,
                Deck = this.DeckService().Deck,
                Marketplace = this.DeckService().Marketplace
            };

            Console.WriteLine("Add TURN DATA, Turns.Count:" + PersistenceData.Turns.Count + " turnNumber:" + turnNumber);
            if (PersistenceData.Turns.Count > turnNumber)
            {
                PersistenceData.Turns[turnNumber] = data;
            }
            else if (PersistenceData.Turns.Count == turnNumber)
            {
                PersistenceData.Turns.Add(data);
            }

            this.DatabaseService().WriteGameSessionMetaDataToDb(MetaData, () => { }, () => { });
            this.DatabaseService().WritePersistenceDataToDb(PersistenceData, () => { }, () => { });
        }

        public void UpdateTurnData(int turnNumber)
        {
            Console.WriteLine("Update Memory with turn number:" + turnNumber);
            AddTurnData(turnNumber);
        }

        public void UpdateTurnData()
        {
            int turnNumber = this.GameRoomService().MatchDTO.TurnNumber;
            Console.WriteLine("Update Memory with turn number:" + turnNumber);
            AddTurnData(turnNumber);
        }

        public void UpdateGameSessionBaseData()
        {
            PersistenceData.GameId = Server.RoomId;
            PersistenceData.ActionLog = _gameRoomService.PlayerActionLog;

            PersistenceData.PlayerIds.Clear();
            foreach (var player in Server.Players)
            {
                PersistenceData.PlayerIds.Add(player.ConnectUserId);
            }
        }

        public void AddActionToActionLog(Message actionMessage)
        {
            this.GameRoomService().PlayerActionLog.AddPlayerAction(actionMessage);
            this.DatabaseService().WritePersistenceDataToDb(PersistenceData, () => { }, () => { });
        }

        public void AddActionToActionLog(PlayerAction action)
        {
            this.GameRoomService().PlayerActionLog.AddPlayerAction(action);
            this.DatabaseService().WritePersistenceDataToDb(PersistenceData, () => { }, () => { });
        }

        public void GenerateInitialGameplayDataDTO(Player player, Action<InitialGameplayDataDTO> successCallback)
        {
            var persistenceData = PersistenceData;
            var playerDto = _gameRoomService.MatchDTO.Players.SingleOrDefault(p => p.PlayerName == player.ConnectUserId);
            if (playerDto == null) return;

            bool initialTurn = true;
            int turnNumber = 0;
            if (this.GameRoomService().MatchDTO.TurnNumber == 0)
            {
                initialTurn = _gameRoomService.MatchDTO.CurrentPlayerIndex != playerDto.PlayerIndex;
            }
            else
            {
                initialTurn = (_gameRoomService.MatchDTO.TurnNumber - playerDto.CurrentTurn) > 1
                    || ((_gameRoomService.MatchDTO.TurnNumber - playerDto.CurrentTurn) == 1 && _gameRoomService.MatchDTO.CurrentPlayerIndex != playerDto.PlayerIndex);
                turnNumber = initialTurn ? playerDto.CurrentTurn + 1 : _gameRoomService.MatchDTO.TurnNumber;
            }

            var turns = initialTurn
                ? persistenceData.InitialTurns
                : persistenceData.Turns;
            var dto = new InitialGameplayDataDTO() { 
                Match = turns[turnNumber].Match,
                HexMap = turns[turnNumber].HexMap,
                Marketplace = turns[turnNumber].Marketplace,
                Deck = turns[turnNumber].Deck,
            };
            dto.ActionLog = persistenceData.ActionLog.DTO;

            successCallback(dto);
        }
    }
}
