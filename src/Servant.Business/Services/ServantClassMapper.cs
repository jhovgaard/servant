using System;
using DapperExtensions.Mapper;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class ServantClassMapper<T> : PluralizedAutoClassMapper<T> where T : class 
    {
        public override void Table(string tableName)
        {
            if (typeof (T) == typeof (Settings))
            {
                TableName = "Settings";
            }

            base.Table(tableName);
        }
    }
}