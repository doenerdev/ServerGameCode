using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIO.GameLibrary;

namespace ServerGameCode
{
    public abstract class DatabaseInteraction<T>
    {
        protected DatabaseObject _databaseEntry;

        public abstract DatabaseObject ToDdObject();
        public abstract void WriteToDb(BigDB dbClient);

        public static T CreateFromDbObject(DatabaseObject dbObject)
        {
            return default(T);
        }
    }
}
