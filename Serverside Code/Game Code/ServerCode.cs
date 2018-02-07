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

namespace ServerGameCode {
	[RoomType("Casual")]
	public class ServerCode : Game<Player>
	{
	    private ServiceContainer _serviceContainer;
	   
	    private TurnManager _turnManager;
	    private bool _gameplayStarted = false;

	    public ServiceContainer ServiceContainer
	    {
	        get { return _serviceContainer; }
	    }
	    
	    public TurnManager TurnManager
	    {
	        get { return _turnManager; }
	    }

		// This method is called when an instance of your the server is created
		public override void GameStarted()
		{
            Console.WriteLine("Server has started: " + RoomId);

            //if this is a continued server, skip waiting for opponents and start right away
            if (IsNewGame() == false)
		    {
                InitializeContinuedGame();
		    }
		    else
		    {
		        InitializeNewGame();
		    }
        }

	    private void InitializeNewGame()
	    {
	        Console.WriteLine("Initializing new server");
            _serviceContainer = new ServiceContainer(this, this.RoomId, RoomData);
            _turnManager = new TurnManager(this);
        }

        private void InitializeContinuedGame()
	    {
	        Console.WriteLine("Initializeing continued server");
	        Visible = false;
	        _serviceContainer = new ServiceContainer(this, this.RoomId, RoomData);
            _turnManager = new TurnManager(this);

            try
	        {
	            PlayerIO.BigDB.Load(
	                "GameSessions",
	                RoomData["GameSessionId"],
	                successCallback: gameRoomInfoDb =>
	                {
	                    //_gameRoomService = GameRoomService.CreateFromDbObject(this, gameRoomInfoDb);
	                    StartGameplay();
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
	        foreach (var player in Players) {
	            ServiceContainer.GameRoomService.AddPlayer(player);
	        }
	    }

	    public void SendMessageToInactivePlayers(Message message)
	    {
	        foreach (var player in Players) {
	            if (player.ConnectUserId != ServiceContainer.GameRoomService.CurrentPlayer.PlayerName)
	            {
	                player.Send(message);
	            }
	        }
	    }

	    private bool IsNewGame()
	    {
            //check whether this is a new server or if this a a continued server session
	        return (RoomData != null && (RoomData.ContainsKey("NewGame") == false || RoomData["NewGame"] != "true")) == false;
	    }

	    public void StartGameplay()
	    {
            Console.WriteLine("Gameplay started");
            SetGameRoomInfoPlayers();
	        ServiceContainer.GameRoomService.GameStartedState = GameStartedState.Started;
	        ServiceContainer.GameRoomService.WriteToDb(PlayerIO.BigDB);
	        _turnManager.Initialize();
	        ServiceContainer.NetworkMessageService.BroadcastPlayerListMessage();
	        ServiceContainer.NetworkMessageService.BroadcastGameStartedMessage();

            _gameplayStarted = true;
        }

	    public override bool AllowUserJoin(Player player)
	    {
            //only allow players to join if the room size permits it
            return PlayerCount < ServiceContainer.GameRoomService.RequiredRoomSize;
	    }

	    // This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		// This method is called whenever a player joins the server
		public override void UserJoined(Player player)
		{
            //TODO bedingungen unterscheiden sich für neues und für fortgesetztes spiel
            Console.WriteLine("Player " + player.Id + " joined the room.");

            foreach (Player pl in Players) {
				if(pl.ConnectUserId != player.ConnectUserId) {
					pl.Send("PlayerJoined", player.ConnectUserId);
				}
			}

		    if (PlayerCount == ServiceContainer.GameRoomService.RequiredRoomSize)
		    {
		        Console.WriteLine("Room " + RoomId + " reached its required room size.");
		        Visible = false; //Make this room invisble once the required room size is reached to prevent this room from showing up in public room lists

                if (_gameplayStarted == false)
		        {
		            StartGameplay();
		        }
		    }
		}

		// This method is called when a player leaves the server
		public override void UserLeft(Player player) {
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

		// This method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message)
		{
		    Console.WriteLine(message.Type);
		    ServiceContainer.NetworkMessageService.GotMessage(player, message);
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
