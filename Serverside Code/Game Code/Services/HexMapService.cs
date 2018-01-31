using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerClientShare.DTO;
using ServerGameCode.Services;

namespace ServerGameCode.Services
{
    public class HexMapService
    {
        private HexCellService _hexCellService;

        public HexMapService(HexCellService hexCellService)
        {
            _hexCellService = hexCellService;
        }

        public HexMapDTO GenerateNewHexMap(HexMapSize size)
        {
            var dto = new HexMapDTO(size);
            var cells = new List<HexCellDTO>();

            for (int z = 0, i = 0; z < dto.Height; z++)
            {
                for (int x = 0; x < dto.Width; x++)
                {
                    cells.Add(_hexCellService.CreateHexCell(x, z, i++));
                }
            }
            dto.Cells = cells;

            return dto;
        } 
    }
}
