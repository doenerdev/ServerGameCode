using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.Enums;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;

namespace ServerGameCode.NetworkMessageHandler
{
    class ClientSentUpdateActionLogIndexHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentUpdateActionLogIndexMessage messageDto = ClientSentUpdateActionLogIndexMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            serviceContainer.GameRoomService.UpdateActionLogIndex(sender, messageDto.ActionLogIndex);
            serviceContainer.PersistenceService.UpdateTurnData();
        }
    }
}
