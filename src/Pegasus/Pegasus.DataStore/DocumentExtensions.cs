using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pegasus.DataStore
{
    public static class DocumentExtensions
    {
        public static async Task<List<T>> ExecuteToListAsync<T>(this IDocumentQuery<T> documentQuery)
        {
            var documentList = new List<T>();
            while (documentQuery.HasMoreResults)
            {
                var documentResult = await documentQuery.ExecuteNextAsync();
                foreach (var trip in documentResult)
                {
                    documentList.Add(trip);
                }
            }

            return documentList;
        }

        public static async Task<T> ExecuteFirstOrDefaultAsync<T>(this IDocumentQuery<T> documentQuery)
        {
            var documentList = await documentQuery.ExecuteToListAsync();
            return documentList.FirstOrDefault();
        }
    }
}
