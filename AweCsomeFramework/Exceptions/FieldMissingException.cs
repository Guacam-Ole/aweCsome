using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365.Exceptions
{
    public class FieldMissingException:Exception
    {
        public FieldMissingException(string message, string fieldname): base(message)
        {
            Data.Add("Field", fieldname);
        }
    }
}
