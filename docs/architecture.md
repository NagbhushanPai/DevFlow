# DevFlow — Architecture Overview

## 1. Overview

DevFlow follows Clean Architecture principles to separate business logic, application use cases, infrastructure concerns, and API delivery.

The backend consists of four primary projects:

* DevFlow.Domain
* DevFlow.Application
* DevFlow.Infrastructure
* DevFlow.API

Additional applications include:

* Angular frontend.
* ASP.NET Core MVC legacy administration module.
* Unit test project.
* Integration test project.

---

## 2. High-Level Architecture

```text
Angular Frontend
       |
       | HTTPS / REST / SignalR
       |
       v
DevFlow.API
       |
       v
DevFlow.Application
       |
       v
DevFlow.Domain

DevFlow.Infrastructure
       |
       +---- SQL Server
       |
       +---- Redis
       |
       +---- Azure Blob Storage
       |
       +---- Email Service
       |
       +---- GitHub API
```

---

## 3. Dependency Rule

Dependencies must point toward the core business logic.

The intended project dependencies are:

```text
DevFlow.Domain
    ^
    |
DevFlow.Application
    ^
    |
DevFlow.Infrastructure
    ^
    |
DevFlow.API
```

More accurately:

* Domain depends on no other DevFlow project.
* Application depends on Domain.
* Infrastructure depends on Application and Domain.
* API depends on Application and Infrastructure.

Business logic must not depend directly on databases, external APIs, or UI frameworks.

---

## 4. DevFlow.Domain

The Domain project contains core business concepts.

Responsibilities include:

* Entities.
* Value objects.
* Domain enums.
* Domain exceptions.
* Domain rules.
* Domain events where required.

Example entities may include:

* User.
* Organization.
* OrganizationMember.
* Team.
* TeamMember.
* Project.
* ProjectMember.
* Issue.
* Comment.
* Sprint.
* ActivityHistory.

The Domain project must not contain:

* Entity Framework Core configuration.
* SQL queries.
* Controllers.
* HTTP-specific logic.
* Angular code.
* External service implementations.

---

## 5. DevFlow.Application

The Application project contains application use cases and orchestration logic.

Responsibilities include:

* Commands.
* Queries.
* DTOs.
* Interfaces.
* Validation.
* Authorization abstractions.
* Application services.
* Use-case orchestration.

The Application project defines abstractions required from external infrastructure.

Examples include:

* Application database context abstraction.
* Email service abstraction.
* File storage abstraction.
* Cache abstraction.
* Current user abstraction.

Infrastructure implementations shall depend on these abstractions.

---

## 6. DevFlow.Infrastructure

The Infrastructure project implements technical and external service concerns.

Responsibilities include:

* Entity Framework Core.
* SQL Server.
* Database migrations.
* Repository implementations where required.
* Identity and authentication infrastructure.
* Redis.
* Azure Blob Storage.
* Email services.
* GitHub API integration.
* External service resilience.

Infrastructure shall implement interfaces defined by the Application layer.

---

## 7. DevFlow.API

The API project is the primary backend entry point.

Responsibilities include:

* API controllers or endpoints.
* HTTP request handling.
* HTTP response handling.
* Dependency injection configuration.
* Authentication middleware.
* Authorization configuration.
* Exception handling middleware.
* Swagger/OpenAPI.
* Health checks.
* Logging configuration.
* Rate limiting.
* SignalR hubs.

The API layer should remain thin.

Business logic must be delegated to the Application and Domain layers.

---

## 8. Angular Frontend

The Angular application provides the primary user interface.

Responsibilities include:

* Authentication UI.
* Dashboard.
* Organization management.
* Team management.
* Project management.
* Issue management.
* Kanban board.
* Sprint management.
* Notifications.
* API communication.
* Client-side route guards.
* Client-side error handling.

The Angular frontend communicates with DevFlow.API.

---

## 9. Legacy Administration Module

The legacy administration module demonstrates maintenance and development using older enterprise web technologies.

Technologies include:

* ASP.NET Core MVC.
* Razor Views.
* jQuery.
* AJAX.

Responsibilities include:

* User administration.
* Organization monitoring.
* Administrative reports.
* System activity views.

---

## 10. Data Storage

SQL Server shall be the primary relational database.

Entity Framework Core shall be used for:

* Database access.
* Entity mapping.
* Migrations.
* Query execution.
* Transaction management.

Redis may be introduced after MVP completion for appropriate caching scenarios.

---

## 11. Authentication and Authorization

Authentication shall use:

* Secure password hashing.
* JWT access tokens.

Authorization shall use:

* Role-based authorization.
* Policy-based authorization.
* Organization membership checks.
* Project membership checks.
* Resource-level authorization where required.

Authorization must always be enforced by the backend.

---

## 12. Error Handling

The backend shall use centralized exception handling.

Errors shall be converted into consistent API responses.

The application should distinguish between:

* Validation errors.
* Authentication errors.
* Authorization errors.
* Resource-not-found errors.
* Business rule violations.
* Conflict errors.
* Unexpected server errors.

Sensitive implementation information shall not be exposed to API clients.

---

## 13. Logging and Observability

The application shall use structured logging.

Logging should capture:

* Application startup.
* Important application operations.
* Authentication failures.
* Authorization failures where appropriate.
* Unexpected exceptions.
* External integration failures.

Health checks shall monitor critical application dependencies.

Azure Application Insights shall be introduced during production deployment.

---

## 14. Testing Architecture

### Unit Tests

Unit tests shall focus on:

* Domain rules.
* Application business logic.
* Validation.
* Important use cases.

### Integration Tests

Integration tests shall focus on:

* API endpoints.
* Authentication.
* Authorization.
* Database operations.
* Critical application workflows.

### Frontend Tests

Frontend tests shall focus on important Angular services, components, and user interactions.

---

## 15. Deployment Architecture

The final application shall use:

* Docker.
* Docker Compose.
* GitHub Actions.
* Microsoft Azure.
* Azure Blob Storage.
* Azure Application Insights.

The CI/CD pipeline shall:

1. Restore dependencies.
2. Build applications.
3. Execute automated tests.
4. Create deployment artifacts or container images.
5. Deploy approved application versions.

---

## 16. Architectural Principles

Development shall follow these principles:

* Clean Architecture.
* SOLID principles.
* Dependency inversion.
* Separation of concerns.
* Explicit authorization.
* API consistency.
* Testability.
* Maintainability.
* Observability.
* Secure-by-default development.

Architectural complexity should only be introduced when it solves a concrete project requirement.
