# Pegasus.Web
Online Booking REST API Service

# Technologies Used

- REST API built using ASP.Net Core and Visual Studio 2017.
- Azure WebJob for background processing of timedout bookings and updating seat availability status.
- DataStore is NoSQL based documents, implemented using CosmosDB (Previously known as DocumentDB).

# Implementation Details

Pegasus Solution is split into three projects.
1. Pegasus.Web 
	- Contains implementation of ASP.Net Core Web API
	- Contains three controllers for various operations.
		- VehiclesController - Manage Vehicle operations
		- TripController - Manage Trip operations
		- BookingController - Manage Booking operations
2. Pegasus.DataStore
	- Contains implementation of CosmosDB data store for json documents
	- Contains three repositories for managing documents
		- VehiclesRepository - Manage Vehicle document
		- TripRepository - Manage Trip document
		- BookingRepository - Manage Booking document
3. Pegasus.WebJob 
	- Contains implementation of timer based Azure WebJob which checks for bookings which have not completed within timeout.
	- For bookings which are timedout, seats blocked for that booking will be made available for new bookings.
4. Pegasus.Test
	- Contains unit tests for controllers

# Prerequisites

1. Visual Studio 2017 - Run as Administrator
2. Download and install the Azure Cosmos DB Emulator from the Microsoft Download Center. This is required to run end to end tests on local machine.
3. Set a local environment variable named AzureWebJobsEnv with value Development 

# API Documentation

Refer Src/Pegasus/PegasusApiDocumentation.json for sample requests for booking APIs. This documentation is compatible with PostMan tool for API testing.
