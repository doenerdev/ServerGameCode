using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.Enums;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.ExtensionMethods;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;

namespace ServerGameCode.NetworkMessageHandler
{
    class ClientSentChangeTurnHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentChangeTurnMessage messageDto = ClientSentChangeTurnMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            Console.WriteLine("Client " + sender.ConnectUserId + " sent change turn:" + messageDto.TurnNumber);
            serviceContainer.GameRoomService.UpdatePlayerTurnNumber(sender, messageDto.TurnNumber);
            serviceContainer.PersistenceService.AddInitialTurnData(messageDto.TurnNumber, messageDto.NextPlayerIndex);



            Message answer = Message.Create(NetworkMessageType.ServerSentChangeTurn.ToString("G"));

            foreach (var player in serviceContainer.GameRoomService.Players)
            {
                if (player != sender)
                {
                    player.Send(answer);
                }
            }
        }
    }
}
