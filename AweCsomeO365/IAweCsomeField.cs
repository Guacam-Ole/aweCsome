using Microsoft.SharePoint.Client;
using System.Reflection;

namespace AweCsomeO365
{
    public interface IAweCsomeField
    {
        void AddFieldToList( List sharePointList, PropertyInfo property);
    }
}
