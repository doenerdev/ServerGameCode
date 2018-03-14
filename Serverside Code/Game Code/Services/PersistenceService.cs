using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Models;
using ServerClientShare.Services;
using ServerGameCode.DTO;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class PersistenceService : IServerAspect
    {
        private GameRoomService _gameRoomService;
        private HexMapService _hexMapService;
        private DeckService _deckService;

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

        public PersistenceService(ServerCode server, GameRoomService gameRoomService, HexMapService hexMapService, DeckService deckService)
        {
            Server = server;
            _gameRoomService = gameRoomService;
            _hexMapService = hexMapService;
            _deckService = deckService;

            GameSessionsPersistenceDataDTO dataDto = new GameSessionsPersistenceDataDTO();
            dataDto.GameId = Server.RoomId;
            dataDto.ActionLog = _gameRoomService.PlayerActionLog;
            
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
            Console.WriteLine("Intial Match Phase:" + data.Match.GamePhase);

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

        public void AddTurnData(int turnNumber)
        {
            var data = new GameSessionTurnDataDTO()
            {
                Match = this.GameRoomService().MatchDTO,
                HexMap = this.HexMapService().CurrentHexMapDto,
                Deck = this.DeckService().Deck,
                Marketplace = this.DeckService().Marketplace
            };

            Console.WriteLine("persitance turn count before:" + PersistenceData.Turns.Count);
            if (PersistenceData.Turns.Count > turnNumber)
            {
                Console.WriteLine("Number ONE");
                PersistenceData.Turns[turnNumber] = data;
            }
            else if (PersistenceData.Turns.Count == turnNumber)
            {
                Console.WriteLine("Number TWO");
                PersistenceData.Turns.Add(data);
            }
            Console.WriteLine("persitance turn count after:" + PersistenceData.Turns.Count);
        }

        public void UpdateTurnData(int turnNumber)
        {
            Console.WriteLine("Update MEmory with turn number:" + turnNumber);
            AddTurnData(turnNumber);
        }

        public void UpdateTurnData()
        {
            int turnNumber = this.GameRoomService().MatchDTO.TurnNumber;
            Console.WriteLine("Update MEmory with turn number:" + turnNumber);
            AddTurnData(turnNumber);
        }

        public void AddActionToActionLog(Message actionMessage)
        {
            this.GameRoomService().PlayerActionLog.AddPlayerAction(actionMessage);
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
                turnNumber = initialTurn ? playerDto.CurrentTurn + 1 : playerDto.CurrentTurn;
            }


            Console.WriteLine("Turns Count:" + persistenceData.Turns.Count);
            Console.WriteLine("Initial Turns Count:" + persistenceData.InitialTurns.Count);

            Console.WriteLine("Initial :" + initialTurn);
            Console.WriteLine("Player Turn Index:" + playerDto.CurrentTurn);
            Console.WriteLine("Match Turn Index:" + _gameRoomService.MatchDTO.TurnNumber);
            Console.WriteLine("Turn Number:" +  turnNumber);

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
