using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerGameCode.Services;

namespace ServerGameCode.Interfaces
{
    public interface INetworkMessageHandler
    {
        void HandleMessage(Player sender, Message message, ServiceContainer serviceContainer);
    }
}
