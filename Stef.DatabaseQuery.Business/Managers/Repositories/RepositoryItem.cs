using Newtonsoft.Json;
using Stef.DatabaseQuery.Business.Enumerations;
using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Repositories
{
    public class RepositoryItem
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("type")]
        public RepositoryItemType Type { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("creationDate")]
        public DateTime CreationDate { get; set; }
        [JsonProperty("creator")]
        public string Creator { get; set; }
        [JsonProperty("modificationDate")]
        public DateTime ModificationDate { get; set; }
        [JsonProperty("modifier")]
        public string Modifier { get; set; }
    }
}
