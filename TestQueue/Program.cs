using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AweCsome;
using AweCsome.Buffer;
using AweCsome.Interfaces;

namespace TestQueue
{
    class Program
    {
        static IAweCsomeHelpers _helpers = new AweCsomeHelpers();
        static void Main(string[] args)
        {


            var queue = new LiteDbQueue(_helpers, "dummy");
            queue.QueueAddCommand(new Command { Action = Command.Actions.CreateTable, TableName = "test" });
            queue.SyncQueue();
        }
    }
}
