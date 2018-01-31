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
        private GameCode _gameInstance;

        public TurnManager(GameCode gameInstance)
        {
            _gameInstance = gameInstance;
        }

        public void Initialize()
        {
            /*string activePlayerId = _gameInstance.GameRoomService.PlayerIds[_gameInstance.GameRoomService.PlayerIds.Count - 1];
            _gameInstance.GameRoomService.SetActivePlayerId(activePlayerId);
            _gameInstance.GameRoomService.WriteActivePlayerToDb(_gameInstance.PlayerIO.BigDB);*/
        }

        public void SetNextActivePlayer()
        {
            /*ReadOnlyCollection<string> playerIds = _gameInstance.GameRoomService.PlayerIds;
            int currentIndex = playerIds.IndexOf(_gameInstance.GameRoomService.CurrentPlayerName);
            string activePlayerId = currentIndex >= playerIds.Count - 1 ? playerIds[0] : playerIds[currentIndex + 1];
            _gameInstance.GameRoomService.SetActivePlayerId(activePlayerId);
            _gameInstance.GameRoomService.WriteActivePlayerToDb(_gameInstance.PlayerIO.BigDB);

            _gameInstance.NetworkMessageService.BroadcastNexActivePlayerMessage(activePlayerId);*/
        }
    }
}
