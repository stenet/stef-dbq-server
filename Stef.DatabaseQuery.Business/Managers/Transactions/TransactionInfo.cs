using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Transactions
{
    public class TransactionInfo
    {
        public TransactionInfo(Guid id, List<IDbTransaction> transactionList, JArray data)
        {
            Id = id;
            TransactionList = transactionList;
            Data = data;

            CreationDate = DateTime.Now;
        }

        public Guid Id { get; private set; }
        public List<IDbTransaction> TransactionList { get; private set; }
        public JArray Data { get; private set; }
        public DateTime CreationDate { get; private set; }

        public JArray Commit()
        {
            foreach (var transaction in TransactionList)
            {
                transaction.Commit();
                transaction.Dispose();
            }

            return Data;
        }
        public void Rollback()
        {
            foreach (var transaction in TransactionList)
            {
                transaction.Rollback();
                transaction.Dispose();
            }
        }
    }
}
