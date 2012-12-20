using System;
using System.Data.SQLite;
using DapperExtensions;
using DapperExtensions.Mapper;

namespace Servant.Business.Services
{
    public class SqlLiteService<T>:IDisposable where T : class 
    {
        public SQLiteConnection Connection { get; set; }

        public SqlLiteService()
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof(ServantClassMapper<>);
            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servant.sqlite");
            Connection = new SQLiteConnection("Data source=" + dbPath);
            Connection.Open();
        }

        public void Dispose()
        {
            if(Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }

        public void Insert(T entity)
        {
            Connection.Insert(entity);
        }

        public void Delete(T entity)
        {
            Connection.Delete(entity);
        }

        public void Update(T entity)
        {
            DapperExtensions.DapperExtensions.Update(Connection, entity);
        }
    }
}