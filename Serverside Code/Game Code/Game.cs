using System;
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
using ServerGameCode.Services;
using Console = System.Console;

namespace ServerGameCode {
	[RoomType("Casual")]
	public class GameCode : Game<Player>
	{
	    private RandomGenerator _rndGenerator;
	    private Die _die;
        private HexCellService _hexCellService;
	    private HexMapService _hexMapService;
	    private NetworkMessageService _networkMessageService;
	    private GameRoomService _gameRoomService;
	    private TurnManager _turnManager;
	    private bool _gameplayStarted = false;

	    public GameRoomService GameRoomService
	    {
	        get { return _gameRoomService; }
	    }
	    public TurnManager TurnManager
	    {
	        get { return _turnManager; }
	    }
	    public NetworkMessageService NetworkMessageService
	    {
	        get { return _networkMessageService; }
	    }

		// This method is called when an instance of your the game is created
		public override void GameStarted()
		{
            Console.WriteLine("Game has started: " + RoomId);

            //if this is a continued game, skip waiting for opponents and start right away
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
	        Console.WriteLine("Initializing new game");
            _rndGenerator = new RandomGenerator();
            _die = new Die(_rndGenerator);
	        _hexCellService = new HexCellService(_die, _rndGenerator);
            _hexMapService = new HexMapService(_hexCellService);
	        _networkMessageService = new NetworkMessageService(this);
	        _gameRoomService = new GameRoomService(this, this.RoomId, RoomData);
            _turnManager = new TurnManager(this);
        }

        private void InitializeContinuedGame()
	    {
	        Console.WriteLine("Initializeing continued game");
	        Visible = false;
	        _networkMessageService = new NetworkMessageService(this);
	        _turnManager = new TurnManager(this);

            try
	        {
	            PlayerIO.BigDB.Load(
	                "GameSessions",
	                RoomData["GameSessionId"],
	                successCallback: gameRoomInfoDb =>
	                {
	                    _gameRoomService = GameRoomService.CreateFromDbObject(this, gameRoomInfoDb);
	                    StartGameplay();
	                },
	                errorCallback: error =>
	                {
	                    Console.WriteLine("An error occured while trying to fetch the game sessions info from the DB");
	                }
	            );
	        }
	        catch
	        {
	            Console.WriteLine("An error occured while trying to fetch the game sessions info from the DB");
	        }
        }

	    private void SetGameRoomInfoPlayers()
	    {
	        foreach (var player in Players) {
	            _gameRoomService.AddPlayer(player);
	        }
	    }

	    public void SendMessageToInactivePlayers(Message message)
	    {
	        foreach (var player in Players) {
	            if (player.ConnectUserId != _gameRoomService.CurrentPlayer.PlayerName)
	            {
	                player.Send(message);
	            }
	        }
	    }

	    private bool IsNewGame()
	    {
            //check whether this is a new game or if this a a continued game session
	        return (RoomData != null && (RoomData.ContainsKey("NewGame") == false || RoomData["NewGame"] != "true")) == false;
	    }

	    public void StartGameplay()
	    {
            Console.WriteLine("Gameplay started");
            SetGameRoomInfoPlayers();
	        _gameRoomService.GameStartedState = GameStartedState.Started;
	        _gameRoomService.WriteToDb(PlayerIO.BigDB);
	        _turnManager.Initialize();
            _networkMessageService.BroadcastPlayerListMessage();
            _networkMessageService.BroadcastGameStartedMessage();

	  


            _gameplayStarted = true;
        }

	    public override bool AllowUserJoin(Player player)
	    {
            //only allow players to join if the room size permits it
            return PlayerCount < _gameRoomService.RequiredRoomSize;
	    }

	    // This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		// This method is called whenever a player joins the game
		public override void UserJoined(Player player)
		{
            //TODO bedingungen unterscheiden sich für neues und für fortgesetztes spiel
            Console.WriteLine("Player " + player.Id + " joined the room.");

            foreach (Player pl in Players) {
				if(pl.ConnectUserId != player.ConnectUserId) {
					pl.Send("PlayerJoined", player.ConnectUserId);
				}
			}

		    if (PlayerCount == _gameRoomService.RequiredRoomSize)
		    {
		        Console.WriteLine("Room " + RoomId + " reached its required room size.");
		        Visible = false; //Make this room invisble once the required room size is reached to prevent this room from showing up in public room lists

                var hexMapDto = _hexMapService.GenerateNewHexMap(HexMapSize.M);
		        HexMapDTO map = new HexMapDTO(HexMapSize.M)
		        {
		            Width = 10,
		            Height = 10,
		            Cells = new List<HexCellDTO>()
		            {
		                new HexCellDTO()
		                {
		                    HexCellType = HexCellType.Forest,
		                    Resource = new TowerResourceDTO(ResourceType.Glass)
		                },
		                new HexCellDTO()
		                {
		                    HexCellType = HexCellType.Forest,
		                },
		            }
		        };
                //_networkMessageService.BroadcastMessage(NetworkMessageType.ServerGameInitialized, map.ToMessageArguments()); //TODO just for testing

                if (_gameplayStarted == false)
		        {
		            StartGameplay();
		        }
		    }
		}

		// This method is called when a player leaves the game
		public override void UserLeft(Player player) {
            Console.WriteLine("Player: " + player.ConnectUserId + " left");
			_networkMessageService.BroadcastPlayerWentOfflineMessage(player.ConnectUserId);
		}

	    public bool IsPlayerOnline(string playerId)
	    {
	        return Players.FirstOrDefault(p => p.ConnectUserId == playerId) != null;
	    }

	    public bool IsAsynchronous()
	    {
	        return _gameRoomService?.RequiredRoomSize == PlayerCount;
	    }

		// This method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message)
		{
		    Console.WriteLine(message.Type);
		    _networkMessageService.GotMessage(player, message);
		}
	}

    public enum GameStartedState
    {
        Pending,
        Started,
        Ended
    }

    public enum RockPaperScissorSymbol
    {
        Rock,
        Paper,
        Scissor,
    }

    public interface IMessageSerializable
    {
        Message SerializeToMessage();
        void PopulateByMessageDeserialization(Message message);
    }
}