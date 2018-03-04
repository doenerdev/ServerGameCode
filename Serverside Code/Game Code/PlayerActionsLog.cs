using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;

namespace ServerGameCode
{
    public class PlayerActionsLog
    {
        private readonly List<PlayerAction> _playerActions;
        private ServerCode _server;
        public const string MessageType = "PlayerActionsLog";

        public IReadOnlyList<PlayerAction> PlayerActions
        {
            get { return _playerActions.AsReadOnly(); }
        }

        public PlayerActionsLog(ServerCode server)
        {
            _playerActions = new List<PlayerAction>();
            _server = server;
        }

        public void AddPlayerAction(PlayerAction action)
        {
            _playerActions.Add(action);
            _server.ServiceContainer.DatabaseService.WriteActionLogToDb(this);
        }

        public void AddPlayerAction(Message message)
        {
            uint offset = 0;
            _playerActions.Add(PlayerAction.FromMessageArguments(message, ref offset));
            _server.ServiceContainer.DatabaseService.WriteActionLogToDb(this);
        }

        public DatabaseObject ToDBObject()
        {
            DatabaseObject dbObject = new DatabaseObject();

            DatabaseArray actionsDB = new DatabaseArray();
            if (_playerActions != null)
            {
                foreach (var action in _playerActions)
                {
                    actionsDB.Add(action.ToDBObject());
                }
            }
            dbObject.Set("PlayerActions", actionsDB);

            return dbObject;
        }

        public static PlayerActionsLog FromDBObject(DatabaseObject dbObject, ServerCode server)
        {
            if (dbObject.Count == 0) return null;

            PlayerActionsLog log = new PlayerActionsLog(server);
     
            var actionsDB = dbObject.GetArray("Cards");
            for (int i = 0; i < actionsDB.Count; i++)
            {
                log._playerActions.Add(PlayerAction.FromDBObject((DatabaseObject)actionsDB[i]));
            }

            return log;
        }

        /* public ReadOnlyCollection<PlayerAction> PlayerCommandsByTurn(int turn)
         {
             if (_playerActions.ContainsKey(turn))
             {
                 return _playerActions[turn].AsReadOnly();
             }

             Console.WriteLine("Error: Tried fetching player commands for non-existing turn number.");
             return null;
         }

         public ReadOnlyCollection<PlayerAction> LatestPlayerCommands()
         {
             return _playerActions[_playerActions.Count].AsReadOnly();
         }

         public PlayerAction LatestPlayerCommand()
         {
             if (_playerActions != null && _playerActions.Count > 0)
             {
                 return _playerActions.Values.Last().LastOrDefault();
             }
             return null;
         }*/
    }
}
