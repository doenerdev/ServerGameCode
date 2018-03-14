using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;
using ServerClientShare.Enums;

namespace ServerGameCode.DTO
{
    public class LeaderMetaDataDTO : DatabaseDTO<LeaderMetaDataDTO>
    {
        public LeaderType LeaderType { get; set; }
        public string Name { get; set; }

        public override Message ToMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public override DatabaseObject ToDBObject()
        {
            var dbObject = new DatabaseObject();
            dbObject.Set("LeaderType", (int) LeaderType);
            dbObject.Set("Name", Name);
            return dbObject;
        }

        public new static LeaderMetaDataDTO FromDBObject(DatabaseObject dbObject)
        {
            var dto = new LeaderMetaDataDTO();
            dto.LeaderType = (LeaderType) dbObject.GetInt("LeaderType");
            dto.Name = dbObject.GetString("Name");
            return dto;
        }
    }
}
