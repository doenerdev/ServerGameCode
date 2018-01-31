using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;
using ServerClientShare.DTO;

namespace ServerGameCode
{
    public class Player : BasePlayer, IMessageSerializable
    {
        public const string MessageType = "PlayerInfo";

        private PlayerDTO _dto;

        public PlayerDTO DTO => _dto;


        public Player() : base()
        {
            
        }

        public Message SerializeToMessage()
        {
            Message message = Message.Create(MessageType);
            message.Add(ConnectUserId);
            return message;
        }

        public void PopulateByMessageDeserialization(Message message)
        {
            try
            {
                if (ConnectUserId != message.GetString(0)) return;
                //do nothing yet
            }
            catch
            {
                
            }
        }
    }
}
