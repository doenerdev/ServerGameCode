using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerGameCode.Interfaces;

namespace ServerGameCode.Services
{
    public class DatabaseService : IServerAspect
    {
        private BigDB _dbClient;

        public ServerCode Server { get; }

        public DatabaseService(BigDB dbClient)
        {
            _dbClient = dbClient;
        }

        public void WriteActionLogToDb(PlayerActionsLog log)
        {
            _dbClient.LoadOrCreate(
                "GameSessions",
                Server.RoomId,
                successCallback: receivedDbObject =>
                {
                    receivedDbObject.Set("PlayerActionLog", log.ToDBObject());
                    receivedDbObject.Save();
                    Console.WriteLine("Sucessfully updated action log in DB");
                },
                errorCallback: error =>
                {
                    Console.WriteLine("Failed to updated action log in DB");
                }
            );
        }
    }
}
