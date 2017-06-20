using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;
using Pegasus.Web;
using Pegasus.Web.Controllers;
using Pegasus.Web.Models;

namespace Pegasus.Test
{
    [TestClass]
    public class TripControllerTests
    {
        private readonly Mock<ITripRepository> _mockTripRepository = new Mock<ITripRepository>();
        private readonly Mock<IVehicleRepository> _mockVehicleRepository = new Mock<IVehicleRepository>();
        private readonly Mock<IBookingRepository> _mockBookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<ILogger<TripController>> _mockLogger = new Mock<ILogger<TripController>>();

        [TestMethod]
        public async Task TripController_Search_WhenTripsFound_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            _mockTripRepository
                .Setup(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Search("CityA", "CityB", DateTime.Now.ToString("MM-dd-yyyy"));
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var tripResponse = objResult.Value as TripResponseModel;
            Assert.IsNotNull(tripResponse);
            Assert.IsTrue(tripResponse.Trips.Count() > 0);
            Assert.AreEqual(tripReference, tripResponse.Trips.FirstOrDefault().TripReference);
            Assert.AreEqual(TripStatus.Scheduled, tripResponse.Trips.FirstOrDefault().TripStatus.ToEnum<TripStatus>());
            _mockTripRepository.Verify(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task TripController_Search_WhenTripsNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            _mockTripRepository
                .Setup(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(default(IEnumerable<Trip>))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Search("CityA", "CityB", DateTime.Now.ToString("MM-dd-yyyy"));

            // Assert
            Assert.IsNotNull(result as NotFoundResult);
            _mockTripRepository.Verify(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task TripController_Search_ForInvalidDate_ShouldReturnBadRequest()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            _mockTripRepository
                .Setup(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Search("CityA", "CityB", DateTime.Now.ToString("MMddyyyy"));

            // Assert
            Assert.IsNotNull(result as BadRequestObjectResult);
            _mockTripRepository.Verify(m => m.GetByTripDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task TripController_GetTripDetails_ShouldReturnMatchingTrip()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.GetTripDetails(tripReference);
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var tripResponse = objResult.Value as TripResponseModel;
            Assert.IsNotNull(tripResponse);
            Assert.AreEqual(1, tripResponse.Trips.Count());
            Assert.AreEqual(tripReference, tripResponse.Trips.FirstOrDefault().TripReference);
            Assert.AreEqual(TripStatus.Scheduled, tripResponse.Trips.FirstOrDefault().TripStatus.ToEnum<TripStatus>());
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task TripController_Add_WhenVehicleExists_ShouldReturnTripReference()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetVehicle(vinReference))
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.AddAsync(It.IsAny<Trip>()))
                .ReturnsAsync(tripReference)
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Add(new TripRequestModel
            {
                FromCity = "CityA",
                ToCity = "CityB",
                DepartureTime = DateTime.Now.AddHours(2),
                ArrivalTime = DateTime.Now.AddHours(10),
                VehicleNumber = vinReference
            });
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var actualTripReference = objResult.Value as string;
            Assert.IsNotNull(actualTripReference);
            Assert.AreEqual(tripReference, actualTripReference);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.AddAsync(It.IsAny<Trip>()), Times.Once);
        }

        [TestMethod]
        public async Task TripController_Add_WhenVehiceDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Vehicle))
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.AddAsync(It.IsAny<Trip>()))
                .ReturnsAsync(tripReference)
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Add(new TripRequestModel
            {
                FromCity = "CityA",
                ToCity = "CityB",
                DepartureTime = DateTime.Now.AddHours(2),
                ArrivalTime = DateTime.Now.AddHours(10),
                VehicleNumber = vinReference
            });

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.AddAsync(It.IsAny<Trip>()), Times.Never);
        }

        [TestMethod]
        public async Task TripController_Reset_WhenBookingExists_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.ResetAsync(It.IsAny<Trip>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Reset(tripReference);

            // Assert
            Assert.IsNotNull(result as OkResult);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.ResetAsync(It.IsAny<Trip>()), Times.Once);
            _mockBookingRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Once);
        }

        [TestMethod]
        public async Task TripController_Reset_WhenBookingDoesNotExist_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.ResetAsync(It.IsAny<Trip>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(IEnumerable<Booking>))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Reset(tripReference);

            // Assert
            Assert.IsNotNull(result as OkResult);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.ResetAsync(It.IsAny<Trip>()), Times.Once);
            _mockBookingRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task TripController_Reset_WhenTripDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Trip))
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.ResetAsync(It.IsAny<Trip>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(IEnumerable<Booking>))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var tripController = new TripController(_mockTripRepository.Object, _mockVehicleRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await tripController.Reset(tripReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.ResetAsync(It.IsAny<Trip>()), Times.Never);
            _mockBookingRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }        
    }
}
