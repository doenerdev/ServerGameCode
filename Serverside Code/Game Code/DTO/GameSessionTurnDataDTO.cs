using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;

namespace ServerGameCode.DTO
{
    public class GameSessionTurnDataDTO : DatabaseDTO<GameSessionTurnDataDTO>
    {
        public MatchDTO Match { get; set; }
        public HexMapDTO HexMap { get; set; }
        public DeckDTO Marketplace { get; set; }
        public DeckDTO Deck { get; set; }

        public override Message ToMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public override DatabaseObject ToDBObject()
        {
            DatabaseObject turnData = new DatabaseObject();
            turnData.Set("Match", Match.ToDBObject());
            turnData.Set("HexMap", HexMap.ToDBObject());
            turnData.Set("Marketplace", Marketplace.ToDBObject());
            turnData.Set("Deck", Deck.ToDBObject());
            return turnData;
        }

        public new static GameSessionTurnDataDTO FromDBObject(DatabaseObject dbObject)
        {
            var dto = new GameSessionTurnDataDTO();
            dto.Match = MatchDTO.FromDBObject(dbObject.GetObject("Match"));
            dto.HexMap = HexMapDTO.FromDBObject(dbObject.GetObject("HexMap"));
            dto.Marketplace = DeckDTO.FromDBObject(dbObject.GetObject("Marketplace"));
            dto.Deck = DeckDTO.FromDBObject(dbObject.GetObject("Deck"));
            return dto;
        }
    }
}
