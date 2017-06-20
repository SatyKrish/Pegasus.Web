using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;
using Pegasus.Web.Models;

namespace Pegasus.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    public class VehiclesController : Controller
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ILogger _logger;

        public VehiclesController(IVehicleRepository vehicleRepository, ILogger<VehiclesController> logger)
        {
            _vehicleRepository = vehicleRepository;
            _logger = logger;
        }

        // GET api/vehicles/getVehicleDetails?vin={vin} 
        [HttpGet]
        public async Task<IActionResult> GetVehicleDetails([FromQuery]string vin)
        {
            var vehicle = await this._vehicleRepository.GetByVinAsync(vin);
            if (vehicle == null)
            {
                return NotFound(vin);
            }

            var vehicleReponse = new VehicleModel
            {
                TrafficServiceProvider = vehicle.Tsp,
                VehicleNumber = vehicle.Vin,
                Make = vehicle.Details.Make,
                Model = vehicle.Details.Model,
                Year = vehicle.Details.Year,
                Seats = vehicle.Seats.Select(s => new VehicleModel.Seat
                {
                    SeatNumber = s.SeatNumber,
                    Position = s.Position.ToString()
                })
            };
            return Ok(vehicleReponse);
        }

        // POST api/vehicles/add
        [HttpPost]
        public async Task<IActionResult> Add([FromBody]VehicleModel vehicleRequest)
        {
            var vehicle = await this._vehicleRepository.GetByVinAsync(vehicleRequest.VehicleNumber);
            if (vehicle != null)
            {
                const int VEHICLE_ALREADY_EXISTS = 1001;
                return StatusCode(VEHICLE_ALREADY_EXISTS);
            }

            var newVehicle = new Vehicle
            {
                Tsp = vehicleRequest.TrafficServiceProvider,
                Vin = vehicleRequest.VehicleNumber,
                Details = new VehicleDetails
                {
                    Make = vehicleRequest.Make,
                    Model = vehicleRequest.Model,
                    Year = vehicleRequest.Year
                },
                Seats = vehicleRequest.Seats.Select(s => new Seat
                {
                    SeatNumber = s.SeatNumber,
                    Position = s.Position.ToEnum<SeatPosition>()               
                }).ToArray()
            };

            await this._vehicleRepository.AddAsync(newVehicle);
            return Ok();
        }        
    }
}
