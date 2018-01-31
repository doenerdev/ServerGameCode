using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;

namespace ServerGameCode.Services
{
    public class NetworkMessageService
    {
        private GameCode _game;

        public NetworkMessageService(GameCode game)
        {
            _game = game;
        }

        public void GotMessage(Player player, Message message)
        {
            NetworkMessageType messageType;
            var parseSuccessful = Enum.TryParse(message.Type, out messageType);

            if (parseSuccessful)
            {
                switch (messageType)
                {
                    /*case "Command":
                        BroadcastCommandMessage(player, message);
                        break;
                    case "PlayerLeft":
                        player.Disconnect();
                        break;*/
                    case NetworkMessageType.RequestHexMap:
                        HexMapDTO map = new HexMapDTO(HexMapSize.M)
                        {
                            Cells = new List<HexCellDTO>()
                            {
                                new HexCellDTO()
                                {
                                    HexCellType = HexCellType.Mountains,
                                    Resource = new TowerResourceDTO(ResourceType.Glass)
                                },
                                new HexCellDTO()
                                {
                                    HexCellType = HexCellType.Desert,
                                },
                            }
                        };
                        Console.WriteLine("-------------");
                        Console.WriteLine(map.Width);
                        Console.WriteLine(map.Height);
                        Message m = Message.Create(NetworkMessageType.ServerSentHexMap.ToString("G"));
                        m = map.ToMessage(m);

                        foreach (var cell in map.Cells)
                        {
                            Console.WriteLine(cell.HexCellType);
                        }

                        uint offset = 0;
                        HexMapDTO dto = HexMapDTO.FromMessageArguments(m, ref offset);
                        Console.WriteLine(dto.Width);
                        Console.WriteLine(dto.Height);

                        foreach (var cell in dto.Cells)
                        {
                            Console.WriteLine(cell.HexCellType);
                        }

                        _game.Broadcast(m);
                        break;
                }
            }
        }

        public void BroadcastMessage(NetworkMessageType type, string content = null)
        {
            Message message = Message.Create(type.ToString("G"));

            if (content != null)
            {
                message.Add(content);
            }

            _game.Broadcast(message);
        }

        public void BroadcastGameStartedMessage()
        {
            Message gameplayStartedMessage = Message.Create("GameplayStarted");
            gameplayStartedMessage.Add(_game.GameRoomService.CurrentPlayer.PlayerName);

            _game.Broadcast(gameplayStartedMessage);
        }

        public void BroadcastPlayerListMessage()
        {
            /*Message playerListMessage = Message.Create("PlayerList");
            foreach (var playerId in _game.GameRoomService.PlayerIds)
            {
                playerListMessage.Add(playerId);
                playerListMessage.Add(_game.IsPlayerOnline(playerId));
            }

            _game.Broadcast(playerListMessage);*/
        }

        public void BroadcastNexActivePlayerMessage(string activePlayerId)
        {
            Message message = Message.Create("NewTurn", activePlayerId);
            _game.Broadcast(message);
        }

        public void BroadcastPlayerWentOfflineMessage(string playerId)
        {
            _game.Broadcast("PlayerOffline", playerId);
        }

        public void BroadcastPlayerCameOnlineMessage(string playerId)
        {
            _game.Broadcast("PlayerOnline", playerId);
        }

        private void BroadcastCommandMessage(Player player, Message message)
        {
            CommandId commandId = (CommandId)Enum.Parse(typeof(CommandId), message.GetString(0));
            PlayerCommand command = PlayerCommand.CreateFromMessageOptions(player, commandId, message.GetString(1));
            _game.GameRoomService.PlayerCommands.AddPlayerCommand(command);
            _game.GameRoomService.WriteCommandLogToDb(_game.PlayerIO.BigDB);

            switch (commandId)
            {
                case CommandId.EndTurn:
                    _game.TurnManager.SetNextActivePlayer();
                    break;
                default:
                    Message commandMessage = Message.Create(message.Type, message.GetString(0));
                    foreach (var connectedPlayer in _game.Players)
                    {
                        if (connectedPlayer.ConnectUserId != player.ConnectUserId)
                        {
                            player.Send(commandMessage);
                        }
                    }
                    break;
            }
            try
            {
                /*CommandId commandId = (CommandId) Enum.Parse(typeof(CommandId), message.GetString(0));
                PlayerCommand command = PlayerCommand.CreateFromString(player, commandId, message.GetString(1));
                _game.GameRoomService.PlayerCommands.AddPlayerCommand(command);
                _game.GameRoomService.WriteCommandLogToDb(_game.PlayerIO.BigDB);

                switch (commandId)
                {
                    case CommandId.EndTurn:
                        _game.TurnManager.SetNextActivePlayer();
                        break;
                    default:
                        Message commandMessage = Message.Create(message.Type, message.GetString(0));
                        foreach (var connectedPlayer in _game.Players)
                        {
                            if (connectedPlayer.ConnectUserId != player.ConnectUserId)
                            {
                                player.Send(commandMessage);
                            }
                        }
                        break;
                }*/
            }
            catch
            {
                Console.WriteLine("Failed at broadcasting command message");
            } 
        }
    }
}
