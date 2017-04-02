using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class SecurityManager
    {
        private static object _SyncLock = new object();
        private static SecurityManager _Instance;
        
        private SecurityManager()
        {
        }

        public static SecurityManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new SecurityManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public string GetCurrentUser()
        {
            return "ADMIN";
        }
    }
}
