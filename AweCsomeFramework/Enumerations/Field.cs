using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsome.Enumerations
{
    public enum DateTimeFormat
    {
        DateTime = 0,
        DateOnly = 1,
        TimeOnly = 2,
        ISO8601 = 3,
        MonthDayOnly = 4,
        MonthYearOnly = 5,
        LongDate = 6,
        UnknownFormat = 7
    }

    public enum DateTimeFieldFriendlyFormatType
    {
        Unspecified = 0,
        Disabled = 1,
        Relative = 2
    }
    public enum RelationshipDeleteBehaviorType
    {
        None = 0,
        Cascade = 1,
        Restrict = 2
    }

    public enum UrlFieldFormatType
    {
        Hyperlink = 0,
        Image = 1
    }

    public enum FieldUserSelectionMode
    {
        PeopleOnly = 0,
        PeopleAndGroups = 1,
        GroupsOnly = 2
    }
}
