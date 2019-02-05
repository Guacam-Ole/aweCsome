using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Exceptions
{
    public class ItemAlreadyExistsException:Exception
    {
        public ItemAlreadyExistsException(string message):base(message) { }
    }
}
