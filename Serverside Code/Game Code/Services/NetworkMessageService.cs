using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class NetworkMessageService : IServerAspect
    {
        private ServerCode _server;

        public ServerCode Server { get { return _server; }}

        public NetworkMessageService(ServerCode server)
        {
            _server = server;
        }

        public void GotMessage(Player player, Message message)
        {
            NetworkMessageType messageType;
            var parseSuccessful = Enum.TryParse(message.Type, out messageType);

            if (parseSuccessful)
            {
                Message answer;
                switch (messageType)
                {
                    /*case "Command":
                        BroadcastCommandMessage(player, message);
                        break;
                    case "PlayerLeft":
                        player.Disconnect();
                        break;*/
                    case NetworkMessageType.RequestDeck:
                        var deck = this.DeckService().Deck;
                        answer = Message.Create(NetworkMessageType.ServerSentDeck.ToString("G"));
                        answer = deck.ToMessage(answer);

                        _server.Broadcast(answer);
                        break;
                    case NetworkMessageType.RequestMarketplace:
                        var marketplace = this.DeckService().Marketplace;
                        answer = Message.Create(NetworkMessageType.ServerSentMarketplace.ToString("G"));
                        answer = marketplace.ToMessage(answer);
                        _server.Broadcast(answer);
                        break;
                    case NetworkMessageType.RequestHexMap:
                        var map = this.HexMapService().CurrentHexMapDto;
                        answer = Message.Create(NetworkMessageType.ServerSentHexMap.ToString("G"));
                        answer = map.ToMessage(answer);

                        _server.Broadcast(answer);
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

            _server.Broadcast(message);
        }

        public void BroadcastGameStartedMessage()
        {
            Message message = Message.Create(NetworkMessageType.ServerSentReady.ToString("G"));
            message = this.GameRoomService().MatchDTO.ToMessage(message);
            _server.Broadcast(message);
        }

        public void BroadcastPlayerListMessage()
        {
            Message message = Message.Create(NetworkMessageType.ServerSentReady.ToString("G"));
            message = this.GameRoomService().MatchDTO.ToMessage(message);
            _server.Broadcast(message);
        }

        public void BroadcastNexActivePlayerMessage(string activePlayerId)
        {
            Message message = Message.Create("NewTurn", activePlayerId);
            _server.Broadcast(message);
        }

        public void BroadcastPlayerWentOfflineMessage(string playerId)
        {
            _server.Broadcast("PlayerOffline", playerId);
        }

        public void BroadcastPlayerCameOnlineMessage(string playerId)
        {
            _server.Broadcast("PlayerOnline", playerId);
        }

        private void BroadcastCommandMessage(Player player, Message message)
        {
            CommandId commandId = (CommandId)Enum.Parse(typeof(CommandId), message.GetString(0));
            PlayerCommand command = PlayerCommand.CreateFromMessageOptions(player, commandId, message.GetString(1));
            this.GameRoomService().PlayerCommands.AddPlayerCommand(command);
            this.GameRoomService().WriteCommandLogToDb(_server.PlayerIO.BigDB);

            switch (commandId)
            {
                case CommandId.EndTurn:
                    _server.TurnManager.SetNextActivePlayer();
                    break;
                default:
                    Message commandMessage = Message.Create(message.Type, message.GetString(0));
                    foreach (var connectedPlayer in _server.Players)
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
                _server.GameRoomService.PlayerCommands.AddPlayerCommand(command);
                _server.GameRoomService.WriteCommandLogToDb(_server.PlayerIO.BigDB);

                switch (commandId)
                {
                    case CommandId.EndTurn:
                        _server.TurnManager.SetNextActivePlayer();
                        break;
                    default:
                        Message commandMessage = Message.Create(message.Type, message.GetString(0));
                        foreach (var connectedPlayer in _server.Players)
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
