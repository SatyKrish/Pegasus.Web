using System;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace Pegasus.DataStore
{ 
    internal static class DocumentClientFactory
    {
        public static DocumentClient Create(IConfiguration configuration)
        {
            var endpointUri = configuration["PegasusDB:EndpointUri"];
            var primaryKey = configuration["PegasusDB:PrimaryKey"];

            return new DocumentClient(new Uri(endpointUri), primaryKey);
        }
    }
}
