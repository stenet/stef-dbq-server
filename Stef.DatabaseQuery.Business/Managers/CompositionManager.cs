using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class CompositionManager
    {
        private static object _SyncLock = new object();
        private static CompositionManager _Instance;

        private CompositionContainer _CompositionContainer;

        private CompositionManager()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            _CompositionContainer = new CompositionContainer(catalog);
        }

        public static CompositionManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new CompositionManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public IEnumerable<TExport> GetInstances<TExport>()
        {
            return _CompositionContainer
                .GetExportedValues<TExport>();
        }
        public TSpecific GetInstance<TExport, TSpecific>()
        {
            return GetInstances<TExport>()
                .OfType<TSpecific>()
                .FirstOrDefault();
        }
        public TExport GetInstance<TExport>(string fullName)
        {
            return GetInstances<TExport>()
                .FirstOrDefault(c => c.GetType().FullName == fullName);
        }
    }
}
