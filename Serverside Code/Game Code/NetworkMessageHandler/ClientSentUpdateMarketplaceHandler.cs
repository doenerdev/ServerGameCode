using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;

namespace ServerGameCode.NetworkMessageHandler
{
    class ClientSentUpdateMarketplaceHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentUpdateMarketplaceMessage messageDto = ClientSentUpdateMarketplaceMessage.FromMessageArguments(message, ref offset);

            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            serviceContainer.DeckService.UpdateMarketplace(messageDto.Marketplace);
            serviceContainer.PersistenceService.UpdateTurnData(messageDto.TurnNumber);
            serviceContainer.DatabaseService.WriteMarketplaceToDb(messageDto.TurnNumber);
        }
    }
}
