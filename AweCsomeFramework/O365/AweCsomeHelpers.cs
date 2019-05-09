using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AweCsome.Interfaces;

namespace AweCsome
{
    public class AweCsomeHelpers : IAweCsomeHelpers
    {
        public int GetId<T>(T entity)
        {
            
            return (int)GetIdProperty<T>().GetValue(entity);
        }

        public void SetId<T>(T entity, int id)
        {
            GetIdProperty<T>().SetValue(entity, id);
        }

        private PropertyInfo GetIdProperty<T>()
        {
            var idProperty = typeof(T).GetProperty("ID", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (idProperty == null) throw new Exception("cannot use buffer without Id of type int");
            if (idProperty.PropertyType != typeof(int)) throw new TypeAccessException("id must be int");
            return idProperty;
        }

        public string GetListName<T>()
        {
            return EntityHelper.GetInternalNameFromEntityType(typeof(T));
        }
    }
}
