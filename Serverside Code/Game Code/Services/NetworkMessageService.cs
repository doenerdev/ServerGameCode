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

        public void GotMessage(Player playerSender, Message message)
        {
            NetworkMessageType messageType;
            var parseSuccessful = Enum.TryParse(message.Type, out messageType);

            Console.WriteLine("Received Message!");
            Console.WriteLine("Message type:" + messageType);
            if (parseSuccessful)
            {
                Message answer;
                uint offset = 0;

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

                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.RequestMarketplace:
                        var marketplace = this.DeckService().Marketplace;
                        answer = Message.Create(NetworkMessageType.ServerSentMarketplace.ToString("G"));
                        answer = marketplace.ToMessage(answer);
                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.RequestHexMap:
                        var map = this.HexMapService().CurrentHexMapDto;
                        answer = Message.Create(NetworkMessageType.ServerSentHexMap.ToString("G"));
                        answer = map.ToMessage(answer);

                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.GameActionPerformed:
                        Console.WriteLine("Received Network Action");

                        _server.ServiceContainer.GameRoomService.PlayerActionLog.AddPlayerAction(message);

                        answer = Message.Create(NetworkMessageType.ServerSentGameAction.ToString("G"));
                        answer.Add(message.GetString(0));
                        answer.Add(message.GetString(1));
                        answer.Add(message.GetString(2));

                        foreach (var player in _server.Players) {
                            if (player != playerSender)
                            {
                                player.Send(answer);
                            }
                        }
                        break;
                    case NetworkMessageType.ChangeTurnPerformed:
                        answer = Message.Create(NetworkMessageType.ServerSentChangeTurn.ToString("G"));

                        foreach (var player in _server.Players)
                        {
                            if (player != playerSender)
                            {
                                player.Send(answer);
                            }
                        }
                        break;
                    case NetworkMessageType.RequestNewTowerSegment:
                        answer = Message.Create(NetworkMessageType.ServerSentNewTowerSegment.ToString("G"));
                        answer = _server.ServiceContainer.ResourceService
                            .GenerateNewTowerSegment()
                            .ToMessage(answer);
                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.ClientSentHexMap:
                        var hexMapDto = HexMapDTO.FromMessageArguments(message, ref offset);
                        Server.ServiceContainer.HexMapService.UpdateHexMap(hexMapDto);
                        Server.ServiceContainer.DatabaseService.WriteHexMapToDb();
                        break;
                    case NetworkMessageType.ClientSentMatch:
                        var matchDto = MatchDTO.FromMessageArguments(message, ref offset);
                        Server.ServiceContainer.GameRoomService.UpdateMatch(matchDto);
                        Server.ServiceContainer.DatabaseService.WriteMatchToDb();
                        break;
                    case NetworkMessageType.ClientSentMarketplace:
                        var marketplaceDto = DeckDTO.FromMessageArguments(message, ref offset);
                        Server.ServiceContainer.DeckService.UpdateMarketplace(marketplaceDto);
                        Server.ServiceContainer.DatabaseService.WriteMarketplaceToDb();
                        break;
                    case NetworkMessageType.ClientSentDeck:
                        var deckDto = DeckDTO.FromMessageArguments(message, ref offset);
                        Server.ServiceContainer.DeckService.UpdateDeck(deckDto);
                        Server.ServiceContainer.DatabaseService.WriteDeckToDb();
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

        public void BroadcastRoomCreatedMessage()
        {
            Message message = Message.Create("RoomCreated");
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

    }
}
