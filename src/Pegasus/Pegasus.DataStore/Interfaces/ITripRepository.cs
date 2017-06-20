using System.Collections.Generic;
using System.Threading.Tasks;
using Pegasus.DataStore.Documents;
using System;

namespace Pegasus.DataStore.Interfaces
{
    public interface ITripRepository
    {
        Task<Trip> GetByTripReferenceAsync(string tripReference);
        Task<IEnumerable<Trip>> GetByTripDetailsAsync(string fromCity, string toCity, DateTime journeyDate);
        Task<string> AddAsync(Trip trip);
        Task UpdateAsync(Trip trip);
        Task ResetAsync(Trip trip);
    }
}
