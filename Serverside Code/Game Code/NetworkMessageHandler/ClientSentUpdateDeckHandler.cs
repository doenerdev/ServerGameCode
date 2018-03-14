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
    class ClientSentUpdateDeckHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentUpdateDeckMessage messageDto = ClientSentUpdateDeckMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            serviceContainer.DeckService.UpdateDeck(messageDto.Deck);
            serviceContainer.PersistenceService.UpdateTurnData(messageDto.TurnNumber);
            serviceContainer.DatabaseService.WriteDeckToDb(messageDto.TurnNumber);
        }
    }
}
