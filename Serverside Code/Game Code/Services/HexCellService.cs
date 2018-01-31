using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerClientShare.DTO;
using ServerClientShare.Enums;
using ServerGameCode.Helper;

namespace ServerGameCode.Services
{
    public class HexCellService
    {
        private Die _die;
        private RandomGenerator _rndGenerator;

        public HexCellService(Die die, RandomGenerator rndGenerator)
        {
            _die = die;
            _rndGenerator = rndGenerator;
        }

        public HexCellDTO CreateHexCell(int x, int z, int i)
        {
            //randomly select tile type
            var rnd = _rndGenerator.RandomRange(0f, 1f);
            HexCellType type = HexCellType.Desert;
            if (rnd >= 0.8f)
            {
                type = HexCellType.Mountains;
            }
            else if (rnd >= 0.58f)
            {
                type = HexCellType.Forest;
            }

            //randomly select resource
            TowerResourceDTO resource = null;
            var resourceRnd = _rndGenerator.RandomRange(0D, 1D);
            if (resourceRnd > 0.75D)
            {
                var die = _die.RollW6();
                resource = die >= 6
                    ? new TowerResourceDTO(ResourceType.Glass)
                    : die >= 4
                        ? new TowerResourceDTO(ResourceType.Stone)
                        : new TowerResourceDTO(ResourceType.Wood);
            }

            return new HexCellDTO()
            {
                HexCellType = type,
                Resource = resource,
            };
        }
    }
}
