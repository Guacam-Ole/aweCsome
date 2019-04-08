using System.Collections.Generic;
using System.Linq;
using AweCsome.Interfaces;

namespace AweCsome.Buffer
{
    public class LiteDbQueue : LiteDb
    {
        public LiteDbQueue(IAweCsomeHelpers helpers, string databaseName, bool queue) : base(helpers, databaseName, queue)
        {
        }

        public void QueueAddCommand(Command command)
        {
            GetCollection<Command>(null).Insert(command);
        }

        public List<Command> QueueRead()
        {
            return GetCollection<Command>(null).FindAll().ToList();
        }
        public void QueueUpdate(Command command)
        {
            GetCollection<Command>(null).Update(command);
        }
    }
}
