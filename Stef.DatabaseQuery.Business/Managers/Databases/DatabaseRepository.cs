using Newtonsoft.Json;
using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class DatabaseRepository
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }
        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}
