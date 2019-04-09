using System;
using System.Data;
using System.Data.Common;
using RepositoryHelpers.DataBase;
using RepositoryHelpers.Utils;

namespace RepositoryHelpers.DataBaseRepository
{
    public class CustomTransaction 

    {
        private readonly Connection _connection;

        public CustomTransaction(Connection connection)
        {
            _connection = connection;
        }


        //DefaultConnection
        private DbConnection _DBConnection;
        private DbConnection DBConnection
        {
            get
            {
                if (_DBConnection == null)
                {
                    try
                    {
                        _DBConnection = _connection.DataBaseConnection;
                    }
                    catch (Exception exception)
                    {
                        throw new Exception(exception.Message);
                    }
                }

                return _DBConnection;
            }
            set
            {
                _DBConnection = value;
            }
        }

        //Default Transaction
        private DbTransaction _transaction;
        private DbTransaction Transaction
        {
            set
            {
                _transaction = value;
            }
            get
            {
                if (_transaction == null)
                {
                    if (DBConnection.State == ConnectionState.Closed)
                        DBConnection.Open();

                    _transaction = DBConnection.BeginTransaction(_connection.Database != DataBaseType.Oracle ? _connection.IsolationLevel
                        : IsolationLevel.ReadCommitted);
                }
                return _transaction;
            }
        }

        //Default Command
        private DbCommand _DBCommand;
        public DbCommand DbCommand
        {
            set
            {
                _DBCommand = value;
            }
            get
            {
                if (_DBCommand == null)
                {
                    _DBCommand = _connection.GetCommand();
                    _DBCommand.Connection = DBConnection;
                }
                return _DBCommand;
            }
        }

        /// <summary>
        /// Start transaction
        /// </summary>
        public void BeginTransaction()
        {
            DbCommand.Transaction = Transaction;
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        public void CommitTransaction()
        {
            if (DbCommand?.Transaction != null)
            {
                DbCommand.Transaction.Commit();
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (DbCommand.Transaction != null)
            {
                DbCommand.Transaction.Rollback();
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                _transaction.Dispose();
                _transaction = null;
            }
        }
     
    }
}
