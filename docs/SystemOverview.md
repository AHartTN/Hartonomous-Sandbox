# System Overview

This document provides a comprehensive overview of the Hartonomous system.

## 1. Hartonomous.Core

The `Hartonomous.Core` project is the central piece of the application. It contains the core domain objects, interfaces, and business logic.

### 1.1. Entities

The `Entities` directory contains the core domain objects of the application.

*   **`Atom.cs`**: Represents the smallest unit of information in the system.
*   **`AtomEmbedding.cs`**: Represents the vector embedding of an atom.
*   **`TensorAtom.cs`**: Represents a tensor, which is a multi-dimensional array of data.
*   **`Concept.cs`**: Represents a semantic concept that has been discovered from the data.
*   **`AudioData.cs`**: Represents audio data.
*   **`Image.cs`**: Represents image data.
*   **`TextDocument.cs`**: Represents text data.
*   **`Video.cs`**: Represents video data.
*   **`Model.cs`**: Represents an AI/ML model.
*   **`ModelLayer.cs`**: Represents a layer in a neural network.
*   **`InferenceRequest.cs`**: Represents a request to perform inference using a model.
*   **`AutonomousImprovementHistory.cs`**: Represents a record of an attempt by the system to improve itself.
*   **`BillingRatePlan.cs`**: Represents a billing rate plan for the system.
*   **`BillingUsageLedger.cs`**: Represents a record of a billing event.
*   **`AtomGraphEdge.cs`**: Represents an edge in the provenance graph.
*   **`AtomGraphNode.cs`**: Represents a node in the provenance graph.

### 1.2. Interfaces

The `Interfaces` directory defines the contracts for services and repositories.

*   **`IModelRepository.cs`**: Defines the contract for the model repository.
*   **`IModelCapabilityService.cs`**: Defines the contract for the model capability service.

### 1.3. Services

The `Services` directory contains the implementation of the business logic.

*   **`ModelCapabilityService.cs`**: Implements the `IModelCapabilityService` interface.

I will now continue to read the rest of the files in the `Hartonomous.Core` project and document them in this file.
