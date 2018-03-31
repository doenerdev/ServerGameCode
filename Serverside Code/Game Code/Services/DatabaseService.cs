using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Models;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class DatabaseService : IServerAspect
    {
        public ServerCode Server { get; private set; }

        public DatabaseService(ServerCode server)
        {
            Server = server;
        }

        public void DoesGameSessionExist(string gameId, Action<bool> callback)
        {
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                gameId,
                successCallback: gameRoomInfoDb => { callback(gameRoomInfoDb == null); },
                errorCallback: error =>
                {
                    callback(false);
                }
            );
        }

        public void DoesGameMetaDataExist(string gameId, Action<bool> callback)
        {
            Server.PlayerIO.BigDB.Load(
                "GameSessionMetaData",
                gameId,
                successCallback: metaData => { callback(metaData == null); },
                errorCallback: error =>
                {
                    Console.WriteLine("Error");
                    callback(true);
                }
            );
        }

        public void LoadPersistenceDataFromDb(Action<GameSessionsPersistenceDataDTO> successCallback, Action errorCallback)
        {
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: gameRoomInfoDb => { successCallback(GameSessionsPersistenceDataDTO.FromDBObject(gameRoomInfoDb)); },
                errorCallback: error =>
                {
                    errorCallback();
                }
            );
        }

        public void LoadMetaDataFromDb(Action<GameSessionMetaDataDTO> successCallback, Action errorCallback)
        {
            Server.PlayerIO.BigDB.Load(
                "GameSessionMetaData",
                Server.RoomId,
                successCallback: gameRoomInfoDb => { successCallback(GameSessionMetaDataDTO.FromDBObject(gameRoomInfoDb)); },
                errorCallback: error =>
                {
                    errorCallback();
                }
            );
        }

        public void WritePlayerGameSessionsToDb(Player player, string gameSessionId, Action successCallback, Action errorCallback)
        {
            var gameSessions = player.PlayerObject.GetArray("GameSessions");
            if (gameSessions == null)
            {
                var gameSession = new DatabaseObject();
                gameSession.Set("GameId", gameSessionId);
                var gameSessionsArray = new DatabaseArray();
                gameSessionsArray.Add(gameSession);
                player.PlayerObject.Set("GameSessions", gameSessionsArray);
            }
            else
            {
                var gameSession = new DatabaseObject();
                gameSession.Set("GameId", gameSessionId);
                gameSessions.Add(gameSession);
            }

            player.PlayerObject.Save();
            successCallback();
        }

        public void WriteInitialPersistenceDataToDb(GameSessionsPersistenceDataDTO dataDto, Action successCallback, Action errorCallback)
        {
            var currentTimeStamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Server.PlayerIO.BigDB.DeleteRange("GameSessions", "ByTimeCreated", null, (currentTimeStamp - 10), null, () =>
            {
                Server.PlayerIO.BigDB.CreateObject(
                    "GameSessions",
                    dataDto.GameId,
                    dataDto.ToDBObject(),
                    successCallback: receivedDbObject =>
                    {
                        Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                        successCallback();
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                        errorCallback();
                    }
                );
            });
        }

        public void WritePersistenceDataToDb(GameSessionsPersistenceDataDTO dataDto, Action successCallback, Action errorCallback)
        {
            Server.PlayerIO.BigDB.DeleteRange("GameSessions", "ByRequiredRoomSize", null, 0, 3, () =>
            {
                Server.PlayerIO.BigDB.LoadOrCreate(
                    "GameSessions",
                    dataDto.GameId,
                    successCallback: receivedDbObject =>
                    {
                        receivedDbObject.Set(dataDto.GameId, dataDto.ToDBObject());
                        receivedDbObject.Save();
                        Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                        successCallback();
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                        errorCallback();
                    }
                );
            });
        }

        public void WriteGameSessionMetaDataToDb(GameSessionMetaDataDTO dataDto, Action successCallback, Action errorCallback)
        {
            Server.PlayerIO.BigDB.DeleteRange("GameSessions", "ByRequiredRoomSize", null, 0, 3, () =>
            {
                Server.PlayerIO.BigDB.LoadOrCreate(
                    "GameSessionMetaData",
                    dataDto.GameId,
                    successCallback: receivedDbObject =>
                    {
                        receivedDbObject.Set(dataDto.GameId, dataDto.ToDBObject());
                        receivedDbObject.Save();
                        Console.WriteLine("Sucessfully wrote Server Room Info to DB");
                        successCallback();
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to write Server Room Info to DB");
                        errorCallback();
                    }
                );
            });
        }

        public void WriteInitialTurnDataToDb(int turnNumber)
        {
            WriteActionLogToDb(this.GameRoomService().PlayerActionLog);
            WriteMatchToDb(turnNumber, true);
            WriteHexMapToDb(turnNumber, true);
            WriteMarketplaceToDb(turnNumber, true);
            WriteDeckToDb(turnNumber, true);
        }

        public void WriteActionLogToDb(PlayerActionsLog log)
        {
            var turnNumber = Server.ServiceContainer.GameRoomService.MatchDTO.TurnNumber;

            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    receivedDbObject.Set("PlayerActionLog", log.ToDBObject());
                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated action log in DB for index:" + turnNumber);
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated action log in DB");
                }
            );
        }

        public void WriteMatchToDb(int turnNumber, bool initialTurnData = false)
        {
            var targetArray = initialTurnData
                ? "InitialTurns"
                : "Turns";

            var matchData = initialTurnData
                ? this.PersistenceService().PersistenceData.InitialTurns[turnNumber].Match.ToDBObject()
                : this.PersistenceService().PersistenceData.Turns[turnNumber].Match.ToDBObject();

            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray(targetArray);
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Match",
                            matchData
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", Server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", Server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", Server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated match in DB for index:" + turnNumber);
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated match in DB");
                }
            );
        }

        public void WriteHexMapToDb(int turnNumber, bool initialTurnData = false)
        {
            var targetArray = initialTurnData
                ? "InitialTurns"
                : "Turns";

            var hexMapData = initialTurnData
                ? this.PersistenceService().PersistenceData.InitialTurns[turnNumber].HexMap.ToDBObject()
                : this.PersistenceService().PersistenceData.Turns[turnNumber].HexMap.ToDBObject();

            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray(targetArray);
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "HexMap",
                            hexMapData
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", Server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", Server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", Server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated hex map in DB for index:" + turnNumber);
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated hex map in DB");
                }
            );
        }

        public void WriteMarketplaceToDb(int turnNumber, bool initialTurnData = false)
        {
            var targetArray = initialTurnData
                ? "InitialTurns"
                : "Turns";

            var marketplaceData = initialTurnData
                ? this.PersistenceService().PersistenceData.InitialTurns[turnNumber].Marketplace.ToDBObject()
                : this.PersistenceService().PersistenceData.Turns[turnNumber].Marketplace.ToDBObject();

            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray(targetArray);
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Marketplace", 
                            marketplaceData
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", Server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", Server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", Server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }
                   
                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated marketplace in DB for index:" + turnNumber);
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated marketplace in DB");
                }
            );
        }

        public void WriteDeckToDb(int turnNumber, bool initialTurnData = false)
        {
            var targetArray = initialTurnData
                ? "InitialTurns"
                : "Turns";

            var deckData = initialTurnData
                ? this.PersistenceService().PersistenceData.InitialTurns[turnNumber].Deck.ToDBObject()
                : this.PersistenceService().PersistenceData.Turns[turnNumber].Deck.ToDBObject();

            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray(targetArray);
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Deck",
                            deckData
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", Server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", Server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", Server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated deck in DB for index:" + turnNumber);
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated deck in DB");
                }
            );
        }
    }
}
