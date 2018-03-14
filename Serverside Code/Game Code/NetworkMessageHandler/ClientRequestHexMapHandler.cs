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
    class ClientRequestHexMapHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            var messageDto = DatalessClientMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            var map = serviceContainer.HexMapService.CurrentHexMapDto;
            Message answer = Message.Create(NetworkMessageType.ServerSentHexMap.ToString("G"));
            answer = map.ToMessage(answer);

            sender.Send(answer);
        }
    }
}
