using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AweCsome.Interfaces;
using log4net;

namespace AweCsome.Buffer
{
    public class LiteDbQueue : LiteDb
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LiteDbQueue(IAweCsomeHelpers helpers, string databaseName) : base(helpers, databaseName, true)
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

        public void CreateTable(Command command)
        {

        }

        public void SyncQueue()
        {
            var queue = QueueRead();
            _log.Info($"Working with queue ({queue.Count} elements)");
            foreach (var command in QueueRead())
            {
                _log.Debug($"storing command {command}");
                string commandAction = command.Action.ToString();
                try
                {
                    MethodInfo method = GetType().GetMethod(commandAction);
                    method.Invoke(this, new object[] { command });
                }
                catch (System.Exception ex)
                {
                    _log.Error($"Cannot find method for action '{commandAction}'");
                    break;
                }
            }
        }

        private void SyncQueueWrite()
        {

        }

        private void SyncQueueRead()
        {
        }
    }
}
