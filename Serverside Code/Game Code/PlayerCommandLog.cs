using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerGameCode
{
    public class PlayerCommandLog
    {
        private readonly Dictionary<int, List<PlayerCommand>> _playerCommands;

        public const string MessageType = "PlayerCommandLog";

        public ReadOnlyDictionary<int, ReadOnlyCollection<PlayerCommand>> PlayerCommands
        {
            get
            {
                if (_playerCommands != null)
                {
                    return new ReadOnlyDictionary<int, ReadOnlyCollection<PlayerCommand>>(_playerCommands.ToDictionary(k => k.Key, v => v.Value.AsReadOnly()));
                }
                return null;
            }
        }

        public PlayerCommandLog()
        {
            _playerCommands = new Dictionary<int, List<PlayerCommand>>();
        }

        public void AddPlayerCommand(PlayerCommand command)
        {
            int index = 0;
            if (LatestPlayerCommand() != null)
            {
                index = command.PlayerId != LatestPlayerCommand().PlayerId
                    ? _playerCommands.Count
                    : _playerCommands.Count - 1;
            }

            if (_playerCommands.ContainsKey(index))
            {
                if (_playerCommands[index] != null)
                {
                    _playerCommands[index].Add(command);
                }
                else
                {
                    _playerCommands[index] = new List<PlayerCommand>();
                    _playerCommands[index].Add(command);
                }
            }
            else
            {
                _playerCommands.Add(index, new List<PlayerCommand>());
                _playerCommands[index].Add(command);
            }
        }

        public ReadOnlyCollection<PlayerCommand> PlayerCommandsByTurn(int turn)
        {
            if (_playerCommands.ContainsKey(turn))
            {
                return _playerCommands[turn].AsReadOnly();
            }

            Console.WriteLine("Error: Tried fetching player commands for non-existing turn number.");
            return null;
        }

        public ReadOnlyCollection<PlayerCommand> LatestPlayerCommands()
        {
            return _playerCommands[_playerCommands.Count].AsReadOnly();
        }

        public PlayerCommand LatestPlayerCommand()
        {
            if (_playerCommands != null && _playerCommands.Count > 0)
            {
                return _playerCommands.Values.Last().LastOrDefault();
            }
            return null;
        }
    }
}
