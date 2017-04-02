using System;
using System.Collections.Generic;
using System.Linq;
using Stef.DatabaseQuery.Business.Interfaces;
using Stef.DatabaseQuery.Business.Managers.Databases;
using Stef.DatabaseQuery.Business.Enumerations;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class DatabaseManager
    {
        private static object _Sync = new object();
        private static DatabaseManager _Instance;

        private Dictionary<int, DatabaseInfo> _DatabaseInfoDic;

        public DatabaseManager()
        {
            _DatabaseInfoDic = new Dictionary<int, DatabaseInfo>();
        }

        public static DatabaseManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_Sync)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new DatabaseManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public void InitializeDatabases()
        {
            foreach (var repository in RepositoryManager.Instance.Get(RepositoryItemType.Database))
            {
                var databaseRepository = JsonConvert.DeserializeObject<DatabaseRepository>(repository.Data);

                var provider = CompositionManager
                    .Instance
                    .GetInstance<IDatabaseProvider>(databaseRepository.ProviderName);

                Add(databaseRepository);
            }
        }

        public DatabaseInfo GetDatabaseInfo(int id)
        {
            DatabaseInfo result;

            _DatabaseInfoDic.TryGetValue(id, out result);
            return result;
        }

        public DatabaseInfo Add(DatabaseRepository databaseRepository)
        {
            var databaseInfo = _DatabaseInfoDic
                .Values
                .FirstOrDefault(c => c.Id == databaseRepository.Id);

            var provider = CompositionManager
                .Instance
                .GetInstance<IDatabaseProvider>(databaseRepository.ProviderName);

            if (databaseInfo == null)
            {
                databaseInfo = new DatabaseInfo(
                    databaseRepository.Id,
                    provider,
                    databaseRepository.Caption,
                    databaseRepository.ConnectionString);

                _DatabaseInfoDic.Add(databaseInfo.Id, databaseInfo);
            }
            else
            {
                databaseInfo.UpdateData(
                    provider,
                    databaseRepository.Caption,
                    databaseRepository.ConnectionString);
            }

            return databaseInfo;
        }
        public IEnumerable<DatabaseRepository> GetDatabases()
        {
            return RepositoryManager
                .Instance
                .Get(RepositoryItemType.Database)
                .Select(c => JsonConvert.DeserializeObject<DatabaseRepository>(c.Data))
                .ToList();
        }
        public void RemoveDatabase(int id)
        {
            _DatabaseInfoDic.Remove(id);

            foreach (var repository in RepositoryManager.Instance.Get(RepositoryItemType.Database))
            {
                var databaseRepository = JsonConvert.DeserializeObject<DatabaseRepository>(repository.Data);

                if (databaseRepository.Id != id)
                    continue;

                RepositoryManager
                    .Instance
                    .Delete(repository.Id);
            }
        }
        public DatabaseRepository UpdateDatabase(DatabaseRepository source)
        {
            var items = RepositoryManager
                .Instance
                .Get(RepositoryItemType.Database)
                .Select(c => new
                {
                    Repository = c,
                    DatabaseRepository = JsonConvert.DeserializeObject<DatabaseRepository>(c.Data)
                })
                .ToList();

            var item = items
                .FirstOrDefault(c => c.DatabaseRepository.Id == source.Id);

            var repository = item?.Repository ?? new Repositories.RepositoryItem();
            repository.Name = source.Caption;

            var databaseRepository = item?.DatabaseRepository ?? new DatabaseRepository();

            if (databaseRepository.Id == 0)
            {
                databaseRepository.Id = items.Count == 0 
                    ? 1 
                    : items.Max(c => c.DatabaseRepository.Id) + 1;
            }

            databaseRepository.Caption = source.Caption;
            databaseRepository.ProviderName = source.ProviderName;
            databaseRepository.ConnectionString = source.ConnectionString;

            repository.Data = JsonConvert.SerializeObject(databaseRepository);

            Add(databaseRepository);

            var id = RepositoryManager
                .Instance
                .Save(repository);

            return databaseRepository;
        }
    }
}
