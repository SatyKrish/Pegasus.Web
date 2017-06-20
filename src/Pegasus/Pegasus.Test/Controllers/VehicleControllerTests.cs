using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Pegasus.DataStore.Interfaces;
using Microsoft.Extensions.Logging;
using Pegasus.Web.Controllers;
using Pegasus.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Pegasus.Web.Models;
using Pegasus.DataStore.Documents;
using System.Collections.Generic;

namespace Pegasus.Test.Controllers
{
    [TestClass]
    public class VehicleControllerTests
    {
        private readonly Mock<IVehicleRepository> _mockVehicleRepository = new Mock<IVehicleRepository>();
        private readonly Mock<ILogger<VehiclesController>> _mockLogger = new Mock<ILogger<VehiclesController>>();

        [TestMethod]
        public async Task VehicleController_GetVehicleDetails_WhenVehicleExists_ShouldReturnOk()
        {
            // Arrange
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetVehicle(vinReference))
                .Verifiable();

            // Act
            var vehiclesController = new VehiclesController(_mockVehicleRepository.Object, _mockLogger.Object);
            var result = await vehiclesController.GetVehicleDetails(vinReference);
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var vehicleResponse = objResult.Value as VehicleModel;
            Assert.AreEqual(vinReference, vehicleResponse.VehicleNumber);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task VehicleController_GetVehicleDetails_WhenVehicleDoesNotExists_ShouldReturnNotFound()
        {
            // Arrange
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Vehicle))
                .Verifiable();

            // Act
            var vehiclesController = new VehiclesController(_mockVehicleRepository.Object, _mockLogger.Object);
            var result = await vehiclesController.GetVehicleDetails(vinReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task VehicleController_Add_WhenVehicleExists_ShouldReturnVehicleAlreadyExistsStatusCode()
        {
            // Arrange
            const int VEHICLE_ALREADY_EXISTS = 1001;
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetVehicle(vinReference))
                .Verifiable();
            _mockVehicleRepository
                .Setup(m => m.AddAsync(It.IsAny<Vehicle>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var vehiclesController = new VehiclesController(_mockVehicleRepository.Object, _mockLogger.Object);
            var result = await vehiclesController.Add(new VehicleModel
            {
                VehicleNumber = vinReference,
                TrafficServiceProvider = "Pegasus Travels",
                Make = "Volvo",
                Model = "Transporter",
                Year = "2016",
                Seats = new List<VehicleModel.Seat>
                {
                    new VehicleModel.Seat
                    {
                        SeatNumber = "1",
                        Position = "Aisle"
                    },
                    new VehicleModel.Seat
                    {
                        SeatNumber = "2",
                        Position = "Middle"
                    },
                    new VehicleModel.Seat
                    {
                        SeatNumber = "3",
                        Position = "Window"
                    }
                }
            });
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(VEHICLE_ALREADY_EXISTS, statusCodeResult.StatusCode);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
            _mockVehicleRepository.Verify(m => m.AddAsync(It.IsAny<Vehicle>()), Times.Never);
        }

        [TestMethod]
        public async Task VehicleController_Add_WhenVehicleDoesNotExists_ShouldReturnVehicleAlreadyExistsStatusCode()
        {
            // Arrange
            const int VEHICLE_ALREADY_EXISTS = 1001;
            var vinReference = StringHelper.RandomString(6);
            _mockVehicleRepository
                .Setup(m => m.GetByVinAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Vehicle))
                .Verifiable();
            _mockVehicleRepository
                .Setup(m => m.AddAsync(It.IsAny<Vehicle>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var vehiclesController = new VehiclesController(_mockVehicleRepository.Object, _mockLogger.Object);
            var result = await vehiclesController.Add(new VehicleModel
            {
                VehicleNumber = vinReference,
                TrafficServiceProvider = "Pegasus Travels",
                Make = "Volvo",
                Model = "Transporter",
                Year = "2016",
                Seats = new List<VehicleModel.Seat>
                {
                    new VehicleModel.Seat
                    {
                        SeatNumber = "1",
                        Position = "Aisle"
                    },
                    new VehicleModel.Seat
                    {
                        SeatNumber = "2",
                        Position = "Middle"
                    },
                    new VehicleModel.Seat
                    {
                        SeatNumber = "3",
                        Position = "Window"
                    }
                }
            });

            // Assert
            Assert.IsNotNull(result as OkResult);
            _mockVehicleRepository.Verify(m => m.GetByVinAsync(It.IsAny<string>()), Times.Once);
            _mockVehicleRepository.Verify(m => m.AddAsync(It.IsAny<Vehicle>()), Times.Once);
        }
    }
}
