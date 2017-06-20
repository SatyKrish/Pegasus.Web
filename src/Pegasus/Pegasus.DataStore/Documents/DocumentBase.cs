using Newtonsoft.Json;
using System;

namespace Pegasus.DataStore.Documents
{
    public class DocumentBase
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public DateTime LastUpdatedDate { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
