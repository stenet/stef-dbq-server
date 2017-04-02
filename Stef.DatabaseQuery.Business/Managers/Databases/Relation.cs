using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class Relation
    {
        public Relation(string parentTable, string parentColumn, string childTable, string childColumn)
        {
            ParentTable = parentTable;
            ParentColumn = parentColumn;
            ChildTable = childTable;
            ChildColumn = childColumn;
        }

        public string ParentTable { get; private set; }
        public string ParentColumn { get; private set; }
        public string ChildTable { get; private set; }
        public string ChildColumn { get; private set; }
    }
}
