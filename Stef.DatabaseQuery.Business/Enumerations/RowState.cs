using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Enumerations
{
    public enum RowState
    {
        Loaded = 0,
        Modified = 1,
        New = 2,
        Deleted = 3
    }
}
