using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;

namespace ServerGameCode.DTO
{
    public class PlayerMetaDataDTO : DatabaseDTO<PlayerMetaDataDTO>
    {
        public string PlayerName;
        public int PlayerIndex;
        public int Score;
        public bool IsOnline;
        public LeaderMetaDataDTO Leader;

        public override Message ToMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public override DatabaseObject ToDBObject()
        {
            DatabaseObject dbObject = new DatabaseObject();
            dbObject.Set("PlayerName", PlayerName);
            dbObject.Set("PlayerIndex", PlayerIndex);
            dbObject.Set("Score", Score);
            dbObject.Set("IsOnline", IsOnline);
            dbObject.Set("Leader", Leader.ToDBObject());
            return dbObject;
        }

        public new static PlayerMetaDataDTO FromDBObject(DatabaseObject dbObject)
        {
            var dto = new PlayerMetaDataDTO();
            dto.PlayerName = dbObject.GetString("PlayerName");
            dto.PlayerIndex = dbObject.GetInt("PlayerIndex");
            dto.Score = dbObject.GetInt("Score");
            dto.IsOnline = dbObject.GetBool("IsOnline");
            dto.Leader = LeaderMetaDataDTO.FromDBObject(dbObject.GetObject("Leader"));
            return dto;
        }
    }
}
