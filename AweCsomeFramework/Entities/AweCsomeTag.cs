using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Entities
{
    public class AweCsomeTag
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string TermStoreName { get; set; }
        public List<AweCsomeTag> Children { get; set; }
    }
}
