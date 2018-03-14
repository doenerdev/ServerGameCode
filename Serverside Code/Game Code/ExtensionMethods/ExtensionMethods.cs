using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerClientShare.Services;
using ServerGameCode.Interfaces;
using ServerGameCode.Services;

namespace ServerGameCode.ExtensionMethods
{
    public static class ExtensionMethods
    {
        public static DeckService DeckService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.DeckService;
        }

        public static HexMapService HexMapService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.HexMapService;
        }

        public static HexCellService HexCellService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.HexCellService;
        }

        public static NetworkMessageService NetworkMessageService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.NetworkMessageService;
        }

        public static ResourceService ResourceService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.ResourceService;
        }

        public static DatabaseService DatabaseService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.DatabaseService;
        }

        public static GameRoomService GameRoomService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.GameRoomService;
        }

        public static PersistenceService PersistenceService(this IServerAspect aspect)
        {
            return aspect.Server.ServiceContainer.PersistenceService;
        }
    }
}
