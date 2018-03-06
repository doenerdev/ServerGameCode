using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Models;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class DatabaseService : IServerAspect
    {
        private ServerCode _server;

        public ServerCode Server
        {
            get { return _server; }
        }

        public DatabaseService(ServerCode server)
        {
            _server = server;
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

                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "PlayerActionLog", 
                            log.ToDBObject()
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated action log in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated action log in DB");
                }
            );
        }

        public void WriteMatchToDb()
        {
     
            var turnNumber = Server.ServiceContainer.GameRoomService.MatchDTO.TurnNumber;
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Match",
                            Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject()
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated match in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated match in DB");
                }
            );
        }

        public void WriteHexMapToDb()
        {
            var turnNumber = Server.ServiceContainer.GameRoomService.MatchDTO.TurnNumber;
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "HexMap", 
                            Server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject()
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated hex map in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated hex map in DB");
                }
            );
        }

        public void WriteMarketplaceToDb()
        {
            var turnNumber = Server.ServiceContainer.GameRoomService.MatchDTO.TurnNumber;
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Marketplace", 
                            Server.ServiceContainer.DeckService.Marketplace.ToDBObject()
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }
                   
                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated marketplace in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated marketplace in DB");
                }
            );
        }

        public void WriteDeckToDb()
        {
            var turnNumber = Server.ServiceContainer.GameRoomService.MatchDTO.TurnNumber;
            Server.PlayerIO.BigDB.Load(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    if (receivedDbObject == null) return;

                    DatabaseArray turns = receivedDbObject.GetArray("Turns");
                    if (turns != null && turns.Count > turnNumber)
                    {
                        DatabaseObject dbGameplayPersistenceData = turns.GetObject(turnNumber);
                        dbGameplayPersistenceData.Set(
                            "Deck", 
                            Server.ServiceContainer.DeckService.Deck.ToDBObject()
                        );
                    }
                    else
                    {
                        DatabaseObject dbGameplayPersistenceData = new DatabaseObject();
                        dbGameplayPersistenceData.Set("Match", Server.ServiceContainer.GameRoomService.MatchDTO.ToDBObject());
                        dbGameplayPersistenceData.Set("HexMap", _server.ServiceContainer.HexMapService.CurrentHexMapDto.ToDBObject());
                        dbGameplayPersistenceData.Set("Marketplace", _server.ServiceContainer.DeckService.Marketplace.ToDBObject());
                        dbGameplayPersistenceData.Set("Deck", _server.ServiceContainer.DeckService.Deck.ToDBObject());
                        dbGameplayPersistenceData.Set("PlayerActionLog", Server.ServiceContainer.GameRoomService.PlayerActionLog.ToDBObject());
                        turns.Add(dbGameplayPersistenceData);
                    }

                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated deck in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated deck in DB");
                }
            );
        }
    }
}
