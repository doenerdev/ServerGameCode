using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerClientShare.Interfaces;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;
using ServerGameCode.NetworkMessageHandler;

namespace ServerGameCode.Services
{
    public class NetworkMessageService : IServerAspect
    {
        private ServerCode _server;

        private readonly Dictionary<NetworkMessageType, INetworkMessageHandler> _networkMessageHandler = new Dictionary<NetworkMessageType, INetworkMessageHandler>()
        {
            { NetworkMessageType.ClientSentMatch, new ClientSentUpdateMatchHandler() },
            { NetworkMessageType.ClientSentHexMap, new ClientSentUpdateHexMapHandler() },
            { NetworkMessageType.ClientSentMarketplace, new ClientSentUpdateMarketplaceHandler() },
            { NetworkMessageType.ClientSentDeck, new ClientSentUpdateDeckHandler() },
            { NetworkMessageType.ClientSentChangeTurn, new ClientSentChangeTurnHandler() },
            { NetworkMessageType.ClientSentActionLogIndex, new ClientSentUpdateActionLogIndexHandler() },
            { NetworkMessageType.GameActionPerformed, new ClientSentActionHandler() },

            { NetworkMessageType.RequestInitialGameplayData, new ClientRequestInitialGameplayDataHandler() },
            { NetworkMessageType.RequestHexMap, new ClientRequestHexMapHandler()},
            { NetworkMessageType.RequestDeck, new ClientRequestDeckHandler() },
            { NetworkMessageType.RequestMarketplace,  new ClientRequestMarketplaceHandler()},
            { NetworkMessageType.RequestActionLog, new ClientRequestActionLogHandler()},
            { NetworkMessageType.RequestNewTowerSegment, new ClientRequestNewTowerSegmentHandler()},

        }; 

        public ServerCode Server { get { return _server; }}

        public NetworkMessageService(ServerCode server)
        {
            _server = server;
        }

        public void GotMessage(Player playerSender, Message message)
        {
            NetworkMessageType messageType;
            var parseSuccessful = Enum.TryParse(message.Type, out messageType);

            Console.WriteLine("Received message of type:" + messageType);
            if (parseSuccessful)
            {
                if (_networkMessageHandler.ContainsKey(messageType))
                {
                    _networkMessageHandler[messageType].HandleMessage(playerSender, message, _server.ServiceContainer);
                }
            }
            else
            {
                if (message.Type == "PlayerLeft")
                {
                    playerSender.Disconnect();
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

        public void SendMessageConfirmedToPlayer(Player receiver, string confirmedMessageId)
        {
            var message = new ServerConfirmedClientMessage(confirmedMessageId).ToMessage();
            receiver.Send(message);
        }

        public void SendMessageToPlayer(Player receiver, IServerPersistenceMessage message)
        {
            receiver.Send(message.ToMessage());
        }

        public void SendRoomCreatedMessage()
        {
            foreach (var player in this.GameRoomService().Players)
            {
                this.PersistenceService().GenerateInitialGameplayDataDTO(player, (initialDataDto) =>
                {
                    Message answer = Message.Create("RoomCreated");
                    answer = initialDataDto.ToMessage(answer);

                    Console.WriteLine("Sending Room Created to player:" + player.ConnectUserId);
                    player.Send(answer);
                });
            }
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
