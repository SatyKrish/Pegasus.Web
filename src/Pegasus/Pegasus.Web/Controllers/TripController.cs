using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pegasus.Web.Models;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;

namespace Pegasus.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TripController : Controller
    {
        private readonly ITripRepository _tripRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger _logger;

        public TripController(ITripRepository tripRepository, IVehicleRepository vehicleRepository, IBookingRepository bookingRepository, ILogger<TripController> logger)
        {
            _tripRepository = tripRepository;
            _vehicleRepository = vehicleRepository;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        // GET api/trip/search?fromCity={fromCity}&toCity={toCity}&travelDate={travelDate}
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery]string fromCity, [FromQuery]string toCity, [FromQuery]string travelDate)
        {
            try
            {
                DateTime journeyDate;
                if (!DateTime.TryParseExact(travelDate, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out journeyDate))
                {
                    return BadRequest(travelDate);
                }

                var trips = await _tripRepository.GetByTripDetailsAsync(fromCity, toCity, travelDate);
                if (trips == null || trips.Count() == 0)
                {
                    return NotFound();
                }

                var tripResponse = new TripResponseModel
                {
                    Trips = trips.Select(t => new TripResponseModel.Trip
                    {
                        TripReference = t.TripReference,
                        TripStatus = t.Status.ToString(),
                        FromCity = t.Details.FromCity,
                        ToCity = t.Details.ToCity,
                        DepartureTime = t.Details.DepartureTime,
                        ArrivalTime = t.Details.ArrivalTime,
                        VehicleDetails = new TripResponseModel.Vehicle
                        {
                            TrafficServiceProvider = t.Tsp,
                            VehicleNumber = t.Vin
                        },
                        Seats = t.Seats.Select(s => new TripResponseModel.Seat
                        {
                            SeatNumber = s.SeatNumber,
                            AvailabilityStatus = s.Status.ToString(),
                            Position = s.Position.ToString()
                        })
                    })
                };

                return Ok(tripResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw; 
            }
        }

        // GET api/trip/getTripDetails?tripReference={tripReference}
        [HttpGet]
        public async Task<IActionResult> GetTripDetails([FromQuery]string tripReference)
        {
            try
            {
                var trip = await _tripRepository.GetByTripReferenceAsync(tripReference);
                if (trip == null)
                {
                    return NotFound(tripReference);
                }

                var tripResponse = new TripResponseModel
                {
                    Trips = new List<TripResponseModel.Trip>
                {
                    new TripResponseModel.Trip
                    {
                        TripReference = trip.TripReference,
                        TripStatus = trip.Status.ToString(),
                        FromCity = trip.Details.FromCity,
                        ToCity = trip.Details.ToCity,
                        DepartureTime = trip.Details.DepartureTime,
                        ArrivalTime = trip.Details.ArrivalTime,
                        VehicleDetails = new TripResponseModel.Vehicle
                        {
                            TrafficServiceProvider = trip.Tsp,
                            VehicleNumber = trip.Vin
                        },
                        Seats = trip.Seats.Select(s => new TripResponseModel.Seat
                        {
                            SeatNumber = s.SeatNumber,
                            AvailabilityStatus = s.Status.ToString(),
                            Position = s.Position.ToString()
                        })
                    }
                }
                };

                return Ok(tripResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }

        // POST api/trip/add
        [HttpPost]
        public async Task<IActionResult> Add([FromBody]TripRequestModel tripRequest)
        {
            try
            {
                // Retrieve seat information from vehicle details
                var vehicle = await this._vehicleRepository.GetByVinAsync(tripRequest.VehicleNumber);
                if (vehicle == null)
                {
                    return NotFound(tripRequest.VehicleNumber);
                }

                // Populate trip information from request model
                var trip = new Trip
                {
                    TripReference = StringHelper.RandomString(8),
                    JourneyDate = tripRequest.DepartureTime.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture),
                    Tsp = vehicle.Tsp,
                    Vin = vehicle.Vin,
                    VehicleName = $"{vehicle.Details.Make} {vehicle.Details.Model}",
                    Details = new TripDetails
                    {
                        FromCity = tripRequest.FromCity,
                        ToCity = tripRequest.ToCity,
                        DepartureTime = tripRequest.DepartureTime,
                        ArrivalTime = tripRequest.ArrivalTime
                    },
                    Seats = vehicle.Seats.Select(s => new Seat
                    {
                        SeatNumber = s.SeatNumber,
                        Position = s.Position,
                        Status = SeatStatus.Available
                    }).ToArray()
                };

                // Add trip information to document store
                var tripReference = await this._tripRepository.AddAsync(trip);
                return Ok(tripReference);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }

        // PUT api/trip/reset
        [HttpPut]
        public async Task<IActionResult> Reset([FromBody]string tripReference)
        {
            try
            {
                var trip = await this._tripRepository.GetByTripReferenceAsync(tripReference);
                if (trip == null)
                {
                    return NotFound(tripReference);
                }

                // Reset trip status and seat status
                await this._tripRepository.ResetAsync(trip);

                // Cancel bookings for this trip
                var bookings = await this._bookingRepository.GetByTripReferenceAsync(tripReference);
                if (bookings?.Count() > 0)
                {
                    var cancelTasks = new List<Task>();
                    foreach (var booking in bookings)
                    {
                        var canceTask = this._bookingRepository.CancelAsync(booking);
                        cancelTasks.Add(canceTask);
                    }

                    // Update bookings in parallel
                    await Task.WhenAll(cancelTasks);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }
    }
}
