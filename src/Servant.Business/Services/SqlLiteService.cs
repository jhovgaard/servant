using System;
using System.Collections.Generic;
using System.Data.SQLite;
using DapperExtensions;
using DapperExtensions.Mapper;

namespace Servant.Business.Services
{
    public class SqlLiteService<T>:IDisposable where T : class 
    {
        public System.Data.SQLite.SQLiteConnection Connection { get; set; }

        public SqlLiteService()
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof(PluralizedAutoClassMapper<>);
            var dbPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "servant.sqlite");
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