using Pegasus.DataStore.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pegasus.DataStore.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking> GetByBookingReferenceAsync(string bookingReference);
        Task<IEnumerable<Booking>> GetByTripReferenceAsync(string tripReference);
        Task<string> AddAsync(Booking booking);
        Task ConfirmAsync(Booking booking);
        Task CancelAsync(Booking booking);
    }
}