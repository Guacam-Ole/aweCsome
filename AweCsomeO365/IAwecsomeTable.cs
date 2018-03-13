using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365
{
    public interface IAweCsomeTable
    {
        void CreateTable<T>();
        void DeleteTable<T>();
        void DeleteTableIfExisting<T>();

        int InsertItem<T>(T entity);

        T SelectItemById<T>(int id) where T : new();
        List<T> SelectAllItems<T>();
        List<T> SelectItemsByLookupId<T>(string fieldName, int lookupId);
        List<T> SelectItemsByString<T>(string fieldName, string queryValue);
        List<T> SelectItemsByNumber<T>(string fieldName, int number);
        List<T> SelectItemsByQuery<T>(string query);

        void UpdateItem<T>(T entity);

        void DeleteItemById<T>(int id);
    }
}
