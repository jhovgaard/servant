using System.Collections.Generic;
using Simple.Data.Extensions;

namespace Servant.Business.Services
{
    public class Service<T> where T : class
    {
        public readonly dynamic Database;
        public dynamic Table;
        protected string TableName;
        
        protected Service() : this(string.Empty)
        {
        }

        protected Service(string tableName)
        {
            TableName = tableName;
            var dbPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "servant.sqlite");

            Database = Simple.Data.Database.OpenFile(dbPath);
            if (string.IsNullOrEmpty(tableName))
                tableName = typeof(T).Name.Pluralize();
            Table = Database[tableName];
        }

        public List<T> GetAll()
        {
            return Table.All().ToList<T>();
        }

        public T GetById(int id)
        {
            return Table.FindById(id);
        }

        //public void Delete(T entity)
        //{
        //    Database.DeleteById(entity.Id);
        //}

        public void DeleteAll()
        {
            Table.DeleteAll();
        }

        public void Insert(T entity)
        {
            Table.Insert(entity);
        }

        public void Insert(IEnumerable<T> entities)
        {
            using (var transaction = Database.BeginTransaction())
            {
                transaction[TableName].Insert(entities);
                transaction.Commit();
            }
            //Table.Insert(entities);
            
        }

        public void Update(T entity)
        {
            Table.UpdateAll(entity);
        }
    }
}