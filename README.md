# Pegasus.Web
Online booking API service

# Technologies Used

- REST API built using ASP.Net Core and Visual Studio 2017.
- DataStore is NoSQL based documents, implemented using CosmosDB (Previously known as DocumentDB).

# Implementation Details

Pegasus Solution is split into three projects.
1. Pegasus.Web 
	- Contains implementation of ASP.Net Core Web API
	- Contains three controllers for various operations.
		a. VehiclesController - Manage Vehicle operations
		b. TripController - Manage Trip operations
		c. BookingController - Manage Booking operations
2. Pegasus.DataStore
	- Contains implementation of CosmosDB data store for json documents
	- Contains three repositories for managing documents
		a. VehiclesRepository - Manage Vehicle document
		b. TripRepository - Manage Trip document
		c. BookingRepository - Manage Booking document
3. Pegasus.Test
	- Contains unit tests for controllers

# Prerequisites

1. Visual Studio 2017
2. Download and install the Azure Cosmos DB Emulator from the Microsoft Download Center. This is required to run end to end tests on local machine.