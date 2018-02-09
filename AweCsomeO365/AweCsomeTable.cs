using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AweCsomeO365
{
    public class AweCsomeTable : IAweCsomeTable
    {
        public void CreateTable(Type entityType)
        {
            throw new NotImplementedException();
        }

        public void DeleteItemById(Type entityType, int id)
        {
            throw new NotImplementedException();
        }

        public void DeleteTable(Type entityType)
        {
            throw new NotImplementedException();
        }

        public void DeleteTableIfExisting(Type entityType)
        {
            throw new NotImplementedException();
        }

        public int InsertItem<T>(T entity)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectAllItems<T>()
        {
            throw new NotImplementedException();
        }

        public T SelectItemById<T>(int id)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByLookupId<T>(string fieldName, int lookupId)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByNumber<T>(string fieldName, int number)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByQuery<T>(string query)
        {
            throw new NotImplementedException();
        }

        public List<T> SelectItemsByString<T>(string fieldName, string queryValue)
        {
            throw new NotImplementedException();
        }

        public void UpdateItem<T>(T entity)
        {
            throw new NotImplementedException();
        }
    }
}
