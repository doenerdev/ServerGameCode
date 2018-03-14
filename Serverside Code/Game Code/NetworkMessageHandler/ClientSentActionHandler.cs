using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.Enums;
using ServerClientShare.Models;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;

namespace ServerGameCode.NetworkMessageHandler
{
    class ClientSentActionHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentActionMessage messageDto = ClientSentActionMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            var action = PlayerAction.FromMessageArguments(messageDto);
            serviceContainer.PersistenceService.AddActionToActionLog(action);

            ServerSentActionMessage answer = new ServerSentActionMessage(messageDto.ActionName, messageDto.PlayerName, messageDto.ActionJson);
            foreach (var player in serviceContainer.GameRoomService.Players)
            {
                if (player != sender)
                {
                    serviceContainer.NetworkMessageService.SendMessageToPlayer(player, answer);
                }
            }
        }
    }
}
