using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public class ScriptParameter
    {
        public ScriptParameter(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public Type Type { get; private set; }
        public string Name { get; private set; }
    }
}
