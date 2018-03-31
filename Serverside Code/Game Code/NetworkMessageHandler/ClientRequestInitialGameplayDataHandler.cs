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
    class ClientRequestInitialGameplayDataHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            var messageDto = DatalessClientMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);
            sender.RequestedInitialData = true;
            Console.WriteLine("Received Gameplay Data Request");
            /*serviceContainer.PersistenceService.GenerateInitialGameplayDataDTO(sender, (initialDataDto) =>
            {
                Console.WriteLine("Initial Hex Map Cells:" + initialDataDto.HexMap.Cells.Count);
                Message answer = Message.Create(NetworkMessageType.ServerSentInitialGameplayData.ToString("G"));
                answer = initialDataDto.ToMessage(answer);

                Console.WriteLine("Answering Gameplay Data Request");
                sender.Send(answer);
            });*/
        }
    }
}
