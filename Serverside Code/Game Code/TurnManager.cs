using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerGameCode
{
    public class TurnManager
    {
        private ServerCode _serverInstance;

        public TurnManager(ServerCode serverInstance)
        {
            _serverInstance = serverInstance;
        }

        public void Initialize()
        {
            /*string activePlayerId = _serverInstance.GameRoomService.PlayerIds[_serverInstance.GameRoomService.PlayerIds.Count - 1];
            _serverInstance.GameRoomService.SetActivePlayerId(activePlayerId);
            _serverInstance.GameRoomService.WriteActivePlayerToDb(_serverInstance.PlayerIO.BigDB);*/
        }

        public void SetNextActivePlayer()
        {
            /*ReadOnlyCollection<string> playerIds = _serverInstance.GameRoomService.PlayerIds;
            int currentIndex = playerIds.IndexOf(_serverInstance.GameRoomService.CurrentPlayerName);
            string activePlayerId = currentIndex >= playerIds.Count - 1 ? playerIds[0] : playerIds[currentIndex + 1];
            _serverInstance.GameRoomService.SetActivePlayerId(activePlayerId);
            _serverInstance.GameRoomService.WriteActivePlayerToDb(_serverInstance.PlayerIO.BigDB);

            _serverInstance.NetworkMessageService.BroadcastNexActivePlayerMessage(activePlayerId);*/
        }
    }
}
