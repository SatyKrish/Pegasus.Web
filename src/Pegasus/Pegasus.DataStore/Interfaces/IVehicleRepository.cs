using Pegasus.DataStore.Documents;
using System.Threading.Tasks;

namespace Pegasus.DataStore.Interfaces
{
    public interface IVehicleRepository
    {
        Task<Vehicle> GetByVinAsync(string vin);
        Task AddAsync(Vehicle vehicle);
        Task UpdateAsync(Vehicle vehicle);
    }
}