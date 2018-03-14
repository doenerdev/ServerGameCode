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
    class ClientRequestNewTowerSegmentHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            var messageDto = DatalessClientMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            Message answer = Message.Create(NetworkMessageType.ServerSentNewTowerSegment.ToString("G"));
            answer = serviceContainer.ResourceService
                .GenerateNewTowerSegment()
                .ToMessage(answer);
            sender.Send(answer);

            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);
        }
    }
}
