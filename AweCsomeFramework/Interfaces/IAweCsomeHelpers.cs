namespace AweCsome.Interfaces
{
    public interface IAweCsomeHelpers
    {
        string GetListName<T>();
        int GetId<T>(T entity);
        void SetId<T>(T entity, int id);
    }
}
