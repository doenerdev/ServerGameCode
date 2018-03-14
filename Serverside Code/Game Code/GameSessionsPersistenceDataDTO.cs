using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Models;
using ServerGameCode.DTO;

namespace ServerGameCode
{
    public class GameSessionsPersistenceDataDTO : DatabaseDTO<GameSessionsPersistenceDataDTO>
    {
        public string GameId { get; set; }
        public List<string> PlayerIds { get; set; }
        public PlayerActionsLog ActionLog { get; set; }
        public List<GameSessionTurnDataDTO> InitialTurns { get; set; }
        public List<GameSessionTurnDataDTO> Turns { get; set; }

        public GameSessionsPersistenceDataDTO()
        {
            PlayerIds = new List<string>();
            InitialTurns = new List<GameSessionTurnDataDTO>();
            Turns = new List<GameSessionTurnDataDTO>();
        }

        public override Message ToMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public override DatabaseObject ToDBObject()
        {
            DatabaseObject dbObject = new DatabaseObject();
            dbObject.Set("GameId", GameId);

            DatabaseArray playerIdsDB = new DatabaseArray();
            if (PlayerIds != null)
            {
                foreach (var playerId in PlayerIds)
                {
                    playerIdsDB.Add(playerId);
                }
            }
            dbObject.Set("PlayerIds", playerIdsDB);

            dbObject.Set("PlayerActionLog", ActionLog.ToDBObject());

            DatabaseArray initialTurnsDB = new DatabaseArray();
            if (Turns != null)
            {
                foreach (var initialTurn in InitialTurns)
                {
                    initialTurnsDB.Add(initialTurn.ToDBObject());
                }
            }
            dbObject.Set("InitialTurns", initialTurnsDB);

            DatabaseArray turnsDB = new DatabaseArray();
            if (Turns != null)
            {
                foreach (var turn in Turns)
                {
                    turnsDB.Add(turn.ToDBObject());
                }
            }
            dbObject.Set("Turns", turnsDB);

            return dbObject;
        }

        public new static GameSessionsPersistenceDataDTO FromDBObject(DatabaseObject dbObject, ServerCode server)
        {
            if(dbObject.Count == 0) return null;

            GameSessionsPersistenceDataDTO dto = new GameSessionsPersistenceDataDTO();
            dto.GameId = dbObject.GetString("GameId");

            var playerIdsDB = dbObject.GetArray("PlayerIds");
            for (int i = 0; i < playerIdsDB.Count; i++)
            {
                dto.PlayerIds.Add(playerIdsDB[i].ToString());
            }

            dto.ActionLog = PlayerActionsLog.FromDBObject(dbObject.GetObject("PlayerActionLog"), server);

            var initialTurnsDB = dbObject.GetArray("InitialTurns");
            foreach (object initialTurn in initialTurnsDB)
            {
                dto.InitialTurns.Add(GameSessionTurnDataDTO.FromDBObject((DatabaseObject)initialTurn));
            }

            var turnsDB = dbObject.GetArray("Turns");
            foreach (object turn in turnsDB)
            {
                dto.Turns.Add(GameSessionTurnDataDTO.FromDBObject((DatabaseObject)turn));
            }

            return dto;
        }
    }
}
