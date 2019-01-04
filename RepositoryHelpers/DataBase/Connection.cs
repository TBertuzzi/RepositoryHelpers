using System.Collections.Generic;
using System.Data.Common;
using System.Data.OracleClient;
using System.Data.SqlClient;
using RepositoryHelpers.Utils;

namespace RepositoryHelpers.DataBase
{
    public sealed class Connection
    {
        public string ConnectionString { get; set; }
        public DataBaseType Database { get; set; }

        internal DbConnection DataBaseConnection
        {
            get
            {
                switch (Database)
                {
                    case DataBaseType.SqlServer:
                        return new SqlConnection(this.ConnectionString);
                    case DataBaseType.Oracle:
                        return new OracleConnection(this.ConnectionString);
                    default: return null;
                }
            }
        }

        internal DbCommand GetCommand(string sql, DbConnection dbConnection)
        {
            switch (Database)
            {
                case DataBaseType.SqlServer:
                    return new SqlCommand(sql, (SqlConnection)dbConnection);
                case DataBaseType.Oracle:
                    return new OracleCommand(sql, (OracleConnection)dbConnection);
                default: return null;
            }
        }

        internal DbDataAdapter GetDataAdapter()
        {
            switch (Database)
            {
                case DataBaseType.SqlServer:
                    return new SqlDataAdapter();
                case DataBaseType.Oracle:
                    return null;
                default: return null;
            }
        }

        internal object GetParameter(KeyValuePair<string, object> parameter)
        {
            switch (Database)
            {
                case DataBaseType.SqlServer:
                    return new SqlParameter($"@{parameter.Key}", parameter.Value);
                case DataBaseType.Oracle:
                    return null;
                default: return null;
            }
        }
    }
}
