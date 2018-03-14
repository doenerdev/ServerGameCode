using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerClientShare.DTO;
using ServerClientShare.PeristenceMessages;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_WII || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS || UNITY_WP_8_1 || UNITY_WSA || UNITY_WSA_8_1 || UNITY_WSA_10_0 || UNITY_WINRT || UNITY_WINRT_8_1 || UNITY_WINRT_10_0
using PlayerIOClient;
#else
using PlayerIO.GameLibrary;
#endif

namespace ServerGameCode.NetworkMessageHandler
{
    public class ClientSentUpdateHexMapHandler : INetworkMessageHandler
    {
        public void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer)
        {
            uint offset = 0;
            ClientSentUpdateHexMapMessage messageDto = ClientSentUpdateHexMapMessage.FromMessageArguments(message, ref offset);
            serviceContainer.NetworkMessageService.SendMessageConfirmedToPlayer(sender, messageDto.Id);

            serviceContainer.HexMapService.UpdateHexMap(messageDto.HexMap);
            serviceContainer.PersistenceService.UpdateTurnData(messageDto.TurnNumber);
            serviceContainer.DatabaseService.WriteHexMapToDb(messageDto.TurnNumber);
        }
    }
}
