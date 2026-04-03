# Backend.Application

This folder contains the application layer of the Planthor Backend project, following the principles of Clean Architecture.

## Application Layer

The application layer is responsible for implementing the use cases and business logic of the system. It acts as an intermediary between the presentation layer (API, UI) and the domain layer.

In Clean Architecture, the application layer is designed to be independent of frameworks and technologies, allowing for easier testing and maintainability.

## ICommand and IQuery

Within the application layer, you will find the usage of `ICommand` and `IQuery` interfaces. These interfaces are commonly used to define the contracts for executing commands and queries, respectively.

- `ICommand`: Represents a command that performs an action or modifies the system's state. It typically encapsulates the input data required for the command and is executed by an appropriate handler.

- `IQuery`: Represents a query that retrieves data from the system without modifying its state. It encapsulates the input parameters for the query and is executed by a query handler.

By using `ICommand` and `IQuery` interfaces, the application layer achieves a separation of concerns and promotes a more modular and testable codebase.

Please refer to the code within this folder for specific implementations of `ICommand` and `IQuery` interfaces.
