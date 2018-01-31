using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;


//TODO: Bevor ein Spieler einen Command an den Server sendet, wandelt er die aktuellen Spieldaten (bestehend aus Map + Figuren + Karten etc.)
//in einen einzigen Hash-String um, der dem Server als Prüfgrundlage gilt (ob der Spieler z.B. gecheatet hat und auf der Client-Seite die
//Spieldaten unerlaubt manipuliert hat


//TODO: Auf der Client-Seite sollten die empfangen Nachrichten in einer Queue gehalten werden, damit diese nach und nach abgearbeitet werden können (falls
//es zu Verzögerungen kommt, so dass der Spieler selbst nicht an die Reihe kommt, bevor die Züge des Gegners abgearbeitet wurden)

namespace ServerGameCode
{
    public class PlayerCommand : IMessageSerializable
    {
        public const string MessageType = "PlayerCommand";
        public const string StringSeperator = "|";

        private string _playerId;
        private CommandId _commandId;
        private string _commandOptionsString;

        public PlayerCommand()
        {

        }

        public PlayerCommand(string playerId, CommandId commandId, string commandOptionsString)
        {
            _playerId = playerId;
            _commandId = commandId;
            _commandOptionsString = commandOptionsString;
        }

        public void SetIDs(string id, CommandId cid)
        {
            _playerId = id;
            _commandId = cid;
        }

        public string PlayerId
        {
            get { return _playerId; }
        }
        public CommandId CommandId
        {
            get { return _commandId; }
        }

        public Message SerializeToMessage()
        {
            Message message = Message.Create(MessageType);
            message.Add(_playerId);
            message.Add(_commandId);
            return message;
        }

        public DatabaseArray ToDbArray()
        {
            DatabaseArray commandArray = new DatabaseArray();
            commandArray.Add(PlayerId);
            commandArray.Add(CommandId.ToString("G"));
            commandArray.Add(_commandOptionsString);
            return commandArray;
        }

        public static PlayerCommand CreateFromDbObject()
        {
            //TODO add the logic to parse a db object to a player command
            return new PlayerCommand();
        }

        public static PlayerCommand CreateFromMessageOptions(Player player, CommandId commandId, string commandOptionsString)
        {
            //Regex KeyValueRegex = new Regex("([^=\\s]+)='([^']*)'");

            Console.WriteLine("Performing Key Value Matching");
            
            //global::CommandId cmdId = (CommandId) Int32.Parse(parsedCommandString["CommandId"]);
            return new PlayerCommand(player.ConnectUserId, commandId, commandOptionsString);
        }

        public void PopulateByMessageDeserialization(Message message)
        {
            try
            {
                _playerId = message.GetString(0);
                _commandId = (CommandId)message.GetInt(1);
            }
            catch
            {
                Console.WriteLine("Tried deserializing message to player command but the message didn't fit the conventions");
            }
        }
    }
}

public enum CommandId
{
    EndTurn,
    WeaponSelection,
    Default,
}
