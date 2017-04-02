using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Managers.Transactions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class TransactionManager
    {
        private static object _Sync = new object();
        private static TransactionManager _Instance;

        private bool _Initialized;
        private Dictionary<Guid, TransactionInfo> _TransactionDic;

        public TransactionManager()
        {
            _TransactionDic = new Dictionary<Guid, TransactionInfo>();
        }

        public static TransactionManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_Sync)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new TransactionManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public void Start()
        {
            if (!_Initialized)
            {
                lock (_Sync)
                {
                    if (_Initialized)
                        return;

                    _Initialized = true;
                }
            }

            Task.Run(() =>
            {
                while (true)
                {
                    _TransactionDic
                        .Where(c => (DateTime.Now - c.Value.CreationDate).TotalSeconds > 30)
                        .ToList()
                        .ForEach(c => RollbackTransaction(c.Key));

                    Thread.Sleep(1000);
                }
            });
        }

        public Guid KeepTransaction(IDbTransaction transaction, JArray data)
        {
            return KeepTransaction(new List<IDbTransaction>() { transaction }, data);
        }
        public Guid KeepTransaction(List<IDbTransaction> transactionList, JArray data)
        {
            var id = Guid.NewGuid();
            _TransactionDic.Add(id, new TransactionInfo(id, transactionList, data));

            return id;
        }
        public object CommitTransaction(Guid id)
        {
            TransactionInfo transactionInfo;
            if (!_TransactionDic.TryGetValue(id, out transactionInfo))
            {
                return new
                {
                    ok = false
                };
            }

            var data = transactionInfo.Commit();
            _TransactionDic.Remove(id);

            return new
            {
                ok = true,
                data = data
            };
        }
        public object RollbackTransaction(Guid id)
        {
            TransactionInfo transactionInfo;
            if (!_TransactionDic.TryGetValue(id, out transactionInfo))
            {
                return new
                {
                    ok = false
                };
            }

            transactionInfo.Rollback();
            _TransactionDic.Remove(id);

            return new
            {
                ok = true
            };
        }
    }
}
