using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerClientShare.Helper;
using ServerGameCode.Helper;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;
using Console = System.Console;

namespace ServerGameCode
{
    [RoomType("Casual")]
    public class ServerCode : Game<Player>
    {
        private ServiceContainer _serviceContainer;

        private bool _gameplayStarted = false;

        public ServiceContainer ServiceContainer
        {
            get
            {
                if (_serviceContainer == null)
                {
                    _serviceContainer = new ServiceContainer(this, this.RoomId, RoomData);
                }
                return _serviceContainer;
            }
        }

        // This method is called when an instance of your the server is created
        public override void GameStarted()
        {
            Console.WriteLine("Server has started: " + RoomId);
            PreloadPlayerObjects = true;

            //if this is a continued server, skip waiting for opponents and start right away
            IsNewGame((newGame) =>
            {
                if (newGame == false)
                {
                    InitializeContinuedGame();
                }
                else
                {
                    InitializeNewGame();
                }
            });
        }

        private void InitializeNewGame()
        {
            Console.WriteLine("Initializing new server");
        }

        private void InitializeContinuedGame()
        {
            Console.WriteLine("Initializeing continued server");

            HideFromMatchmaking();

            try
            {
                PlayerIO.BigDB.Load(
                    "GameSessions",
                    RoomData["GameSessionId"],
                    successCallback: gameRoomInfoDb =>
                    {
                        _serviceContainer = new ServiceContainer(gameRoomInfoDb, this, this.RoomId, RoomData);
                        Console.WriteLine("Fetched game sesson info from db");
                    },
                    errorCallback: error =>
                    {
                        Console.WriteLine("An error occured while trying to fetch the server sessions info from the DB");
                    }
                );
            }
            catch
            {
                Console.WriteLine("An error occured while trying to fetch the server sessions info from the DB");
            }
        }

        private void SetGameRoomInfoPlayers()
        {
            foreach (var player in Players)
            {
                ServiceContainer.GameRoomService.AddPlayer(player);
            }
        }

        public void SendMessageToInactivePlayers(Message message)
        {
            foreach (var player in Players)
            {
                if (player.ConnectUserId != ServiceContainer.GameRoomService.CurrentPlayer.PlayerName)
                {
                    player.Send(message);
                }
            }
        }

        public void IsNewGame(Action<bool> callback)
        {
            PlayerIO.BigDB.Load(
                "GameSessions",
                this.RoomId,
                successCallback: gameRoomInfoDb => { callback(gameRoomInfoDb == null); },
                errorCallback: error =>
                {
                    callback(false);
                }
            );

        }

        private void StartInitialGameplay()
        {
            foreach (var player in Players)
            {
                var gameSessions = player.PlayerObject.GetArray("GameSessions");
                if (gameSessions == null)
                {
                    var gameSession = new DatabaseObject();
                    gameSession.Set("GameId", RoomId);
                    gameSession.Set("GameStartedState", _serviceContainer.GameRoomService.GameStartedState.ToString("G"));
                    gameSession.Set("Marketplace", _serviceContainer.GameRoomService.MatchDTO.ToDBObject());
                    var gameSessionsArray = new DatabaseArray();
                    gameSessionsArray.Add(gameSession);
                    player.PlayerObject.Set("GameSessions", gameSessionsArray);
                }
                else
                {
                    var gameSession = new DatabaseObject();
                    gameSession.Set("GameId", RoomId);
                    gameSessions.Add(gameSession);
                }
                player.PlayerObject.Save();
            }

            Console.WriteLine("Gameplay started");
            SetGameRoomInfoPlayers();
            ServiceContainer.GameRoomService.GameStartedState = GameStartedState.Started;
            ServiceContainer.GameRoomService.WriteInitialDataToDb(PlayerIO.BigDB, () =>
            {
                ServiceContainer.NetworkMessageService.BroadcastPlayerListMessage();
                ServiceContainer.NetworkMessageService.SendRoomCreatedMessage();
            }); //TODO add again later

            _gameplayStarted = true;
        }

        private void StartContinuedGameplay()
        {
            Console.WriteLine("Gameplay started");
            SetGameRoomInfoPlayers();
            ServiceContainer.NetworkMessageService.SendRoomCreatedMessage();

            _gameplayStarted = true;
        }

        public override bool AllowUserJoin(Player player)
        {
            //only allow players to join if the room size permits it
            return PlayerCount < ServiceContainer.GameRoomService.RequiredRoomSize;
        }

        // This method is called when the last player leaves the room, and it's closed down.
        public override void GameClosed()
        {
            Console.WriteLine("RoomId: " + RoomId);
        }

        // This method is called whenever a player joins the server
        public override void UserJoined(Player player)
        {
            //TODO bedingungen unterscheiden sich für neues und für fortgesetztes spiel
            Console.WriteLine("Player " + player.Id + " joined the room.");

            foreach (Player pl in Players)
            {
                if (pl.ConnectUserId != player.ConnectUserId)
                {
                    pl.Send("PlayerJoined", player.ConnectUserId);
                }
            }

            IsNewGame((newGame) =>
            {
                if (newGame)
                {
                    if (PlayerCount == ServiceContainer.GameRoomService.RequiredRoomSize)
                    {
                        Console.WriteLine("NEW GAME!!!!!!!!!!");
                        Console.WriteLine("Room " + RoomId + " reached its required room size.");
                        HideFromMatchmaking();
                        StartInitialGameplay();
                    }
                }
                else
                {
                    Console.WriteLine("CONTINUED GAME!!!!!!!!!");
                    HideFromMatchmaking();
                    StartContinuedGameplay();
                }
            });
        }

        // This method is called when a player leaves the server
        public override void UserLeft(Player player)
        {
            Console.WriteLine("Player: " + player.ConnectUserId + " left");
            ServiceContainer.NetworkMessageService.BroadcastPlayerWentOfflineMessage(player.ConnectUserId);
        }

        public bool IsPlayerOnline(string playerId)
        {
            return Players.FirstOrDefault(p => p.ConnectUserId == playerId) != null;
        }

        public bool IsAsynchronous()
        {
            return ServiceContainer.GameRoomService?.RequiredRoomSize == PlayerCount;
        }

        public void HideFromMatchmaking()
        {
            Visible = false;
        }

        // This method is called when a player sends a message into the server code
        public override void GotMessage(Player player, Message message)
        {
            Console.WriteLine(message.Type);
            ServiceContainer.NetworkMessageService.GotMessage(player, message);
        }

        public Player GetPlayerByName(string playerName)
        {
            return Players?.SingleOrDefault(p => p.ConnectUserId == playerName);
        }
    }

    public enum GameStartedState
    {
        Pending,
        Started,
        Ended
    }

    public interface IMessageSerializable
    {
        Message SerializeToMessage();
        void PopulateByMessageDeserialization(Message message);
    }
}
