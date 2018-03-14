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
                var playerSenderDto = this.GameRoomService().MatchDTO.Players.Where(p => p.PlayerName == playerSender.ConnectUserId);
                Message answer;
                uint offset = 0;
                int turnNumber = 0;

                switch (messageType)
                {
                    /*case "Command":
                        BroadcastCommandMessage(player, message);
                        break;
                    */
                    case NetworkMessageType.RequestInitialGameplayData:
                        Console.WriteLine("Received Gameplay Data Request");
                        this.PersistenceService().GenerateInitialGameplayDataDTO(playerSender, (initialDataDto) =>
                        {
                            answer = Message.Create(NetworkMessageType.ServerSentInitialGameplayData.ToString("G"));
                            answer = initialDataDto.ToMessage(answer);

                            Console.WriteLine("Answering Gameplay Data Request");
                            playerSender.Send(answer);
                        });
                        break;
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
                    case NetworkMessageType.RequestActionLog:
                        answer = Message.Create(NetworkMessageType.ServerSentDeck.ToString("G"));
                        answer = this.GameRoomService().PlayerActionLog.DTO.ToMessage(answer);

                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.GameActionPerformed:
                        Console.WriteLine("Received Network Action");

                        this.PersistenceService().AddActionToActionLog(message);
                        //this.GameRoomService().PlayerActionLog.AddPlayerAction(message);

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
                   
                    case NetworkMessageType.RequestNewTowerSegment:
                        answer = Message.Create(NetworkMessageType.ServerSentNewTowerSegment.ToString("G"));
                        answer = this.ResourceService()
                            .GenerateNewTowerSegment()
                            .ToMessage(answer);
                        playerSender.Send(answer);
                        break;
                    case NetworkMessageType.ClientSentHexMap:
                        turnNumber = message.GetInt(offset++);
                        var hexMapDto = HexMapDTO.FromMessageArguments(message, ref offset);
                        this.HexMapService().UpdateHexMap(hexMapDto);
                        this.PersistenceService().UpdateTurnData(turnNumber);
                        this.DatabaseService().WriteHexMapToDb(this.GameRoomService().MatchDTO.TurnNumber);
                        break;
                    case NetworkMessageType.ClientSentMatch:
                        turnNumber = message.GetInt(offset++);
                        var matchDto = MatchDTO.FromMessageArguments(message, ref offset);
                        this.GameRoomService().UpdateMatch(playerSender, matchDto);
                        this.PersistenceService().UpdateTurnData(turnNumber);
                        Console.WriteLine("WRITE MATCH TO DB with turn number:" + this.GameRoomService().MatchDTO.TurnNumber);
                        this.DatabaseService().WriteMatchToDb(this.GameRoomService().MatchDTO.TurnNumber);
                        break;
                    case NetworkMessageType.ClientSentMarketplace:
                        turnNumber = message.GetInt(offset++);
                        var marketplaceDto = DeckDTO.FromMessageArguments(message, ref offset);
                        this.DeckService().UpdateMarketplace(marketplaceDto);
                        this.PersistenceService().UpdateTurnData(turnNumber);
                        this.DatabaseService().WriteMarketplaceToDb(this.GameRoomService().MatchDTO.TurnNumber);
                        break;
                    case NetworkMessageType.ClientSentDeck:
                        turnNumber = message.GetInt(offset++);
                        var deckDto = DeckDTO.FromMessageArguments(message, ref offset);
                        this.DeckService().UpdateDeck(deckDto);
                        this.PersistenceService().UpdateTurnData(turnNumber);
                        this.DatabaseService().WriteDeckToDb(this.GameRoomService().MatchDTO.TurnNumber);
                        break;
                    case NetworkMessageType.ClientSentActionLogIndex:
                        var actionLogIndex = message.GetInt(0);
                        this.GameRoomService().UpdateActionLogIndex(playerSender, actionLogIndex);
                        this.PersistenceService().UpdateTurnData();
                        //this.DatabaseService().WriteMatchToDb();
                        break;
                    case NetworkMessageType.ClientSentChangeTurn:
                        Console.WriteLine("Client " + playerSender.ConnectUserId +" sent change turn:" + message.GetInt(0));
                        this.GameRoomService().UpdatePlayerTurnNumber(playerSender, message.GetInt(0));

                        turnNumber = message.GetInt(offset++) + 1;
                        var nextPlayerIndex = message.GetInt(offset++);
                        this.PersistenceService().AddInitialTurnData(turnNumber, nextPlayerIndex); //TODO intial turn data is wrong (currently from the end of the turn before)
                        //this.PersistenceService().AddTurnData(message.GetInt(0));
                        //this.DatabaseService().WriteInitialTurnDataToDb(this.GameRoomService().MatchDTO.TurnNumber);

                        answer = Message.Create(NetworkMessageType.ServerSentChangeTurn.ToString("G"));

                        foreach (var player in _server.Players)
                        {
                            if (player != playerSender)
                            {
                                player.Send(answer);
                            }
                        }
                        break;
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
            /*Console.WriteLine("Broadcast Room Created");
            Message message = Message.Create("RoomCreated");
            message = this.GameRoomService().MatchDTO.ToMessage(message);
            _server.Broadcast(message);*/
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
