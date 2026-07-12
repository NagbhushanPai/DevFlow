# DevFlow — Enterprise Project & Issue Management Platform

DevFlow is a full-stack enterprise project and issue management platform designed to help software teams manage organizations, teams, projects, tasks, bugs, sprints, and development workflows.

Inspired by tools such as Jira and Azure DevOps, DevFlow demonstrates the design, development, testing, deployment, and maintenance of a production-style application using the .NET and Angular ecosystem.

The project focuses on enterprise software engineering concepts including Clean Architecture, RESTful APIs, role-based authorization, real-time communication, third-party integrations, automated testing, CI/CD, cloud deployment, monitoring, and performance optimization.

---

## Table of Contents

- [Overview](#overview)
- [Objectives](#objectives)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [System Architecture](#system-architecture)
- [Solution Structure](#solution-structure)
- [Core Modules](#core-modules)
- [User Roles](#user-roles)
- [Issue Workflow](#issue-workflow)
- [Database Design](#database-design)
- [API Design](#api-design)
- [Authentication and Authorization](#authentication-and-authorization)
- [Real-Time Communication](#real-time-communication)
- [Third-Party Integrations](#third-party-integrations)
- [Legacy Admin Module](#legacy-admin-module)
- [Testing Strategy](#testing-strategy)
- [Performance Optimization](#performance-optimization)
- [Security](#security)
- [CI/CD Pipeline](#cicd-pipeline)
- [Azure Deployment](#azure-deployment)
- [Development Roadmap](#development-roadmap)
- [Getting Started](#getting-started)
- [Environment Configuration](#environment-configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Project Status](#project-status)
- [Future Improvements](#future-improvements)
- [Contributing](#contributing)
- [License](#license)

---

# Overview

DevFlow provides development teams with a centralized platform for managing software projects and development workflows.

Users can create organizations, manage teams, create projects, track issues and bugs, assign work, organize sprints, collaborate through comments, receive real-time updates, upload attachments, and monitor project progress.

The application is being developed using a modern enterprise technology stack centered around ASP.NET Core, Angular, Microsoft SQL Server, Azure, and automated DevOps workflows.

---

# Objectives

The primary objectives of DevFlow are to:

- Build a scalable full-stack application using ASP.NET Core and Angular.
- Design RESTful APIs following industry best practices.
- Implement Clean Architecture and SOLID principles.
- Build secure authentication and authorization mechanisms.
- Implement multi-tenant organization-based data isolation.
- Integrate third-party APIs and cloud services.
- Implement real-time communication using SignalR.
- Apply automated testing strategies.
- Implement performance optimization techniques.
- Build automated CI/CD pipelines.
- Deploy and monitor the application on Microsoft Azure.
- Demonstrate the complete software development lifecycle.

---

# Key Features

## Authentication and Authorization

- User registration
- User login
- JWT-based authentication
- Secure password hashing
- Role-based authorization
- Protected API endpoints
- Token validation and expiration handling

## Organization Management

- Create organizations
- Update organization information
- Add organization members
- Remove organization members
- Manage member roles
- Organization-level data isolation

## Team Management

- Create development teams
- Add team members
- Remove team members
- Assign users to teams
- Manage team membership

## Project Management

- Create projects
- Update projects
- Archive projects
- Manage project members
- Assign teams to projects
- View project activity

## Issue and Bug Tracking

Users can create and manage different types of work items.

Supported issue types include:

- Task
- Bug
- Story
- Improvement

Each issue can contain:

- Title
- Description
- Issue type
- Priority
- Status
- Assignee
- Reporter
- Due date
- Sprint
- Comments
- Attachments
- Activity history

## Kanban Board

The Kanban board provides a visual representation of the development workflow.

Default columns:

```text
Backlog
   ↓
To Do
   ↓
In Progress
   ↓
In Review
   ↓
Done
```

Features include:

- Drag-and-drop issue movement
- Issue cards
- Status updates
- Assignee filtering
- Priority filtering
- Real-time updates

## Sprint Management

- Create sprints
- Define sprint goals
- Add issues to sprint backlog
- Remove issues from sprints
- Start sprints
- Complete sprints
- Track active sprints
- View completed sprint history

## Collaboration

- Issue comments
- Activity history
- Real-time updates
- User notifications
- Email notifications

## Search and Filtering

- Search issues
- Filter by status
- Filter by priority
- Filter by assignee
- Filter by issue type
- Sort results
- Server-side pagination

## File Management

- Upload issue attachments
- Store files using Azure Blob Storage
- Download attachments
- Delete attachments
- Validate file size and type

## Dashboard and Analytics

The dashboard will provide information such as:

- Total projects
- Active projects
- Total issues
- Open issues
- Completed issues
- Issues by status
- Issues by priority
- Sprint progress
- Team activity

## Audit Logging

Important system actions will be recorded, including:

- User creation
- Organization changes
- Team changes
- Project creation
- Issue creation
- Issue assignment
- Issue status changes
- Sprint lifecycle changes

---

# Technology Stack

## Frontend

- Angular 17+
- TypeScript
- HTML5
- CSS / SCSS
- Angular Router
- Angular Reactive Forms
- Angular HTTP Client
- Angular CDK Drag and Drop
- RxJS

## Backend

- C#
- ASP.NET Core Web API
- Entity Framework Core
- ASP.NET Core Identity
- SignalR
- FluentValidation
- Swagger / OpenAPI

## Database

- Microsoft SQL Server

## Caching

- Redis
- ASP.NET Core In-Memory Cache

## Testing

### Backend

- xUnit
- Moq
- ASP.NET Core Integration Testing

### Frontend

- Angular Unit Testing Framework

## Cloud

- Microsoft Azure
- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Azure Application Insights

## DevOps

- Git
- GitHub
- GitHub Actions / Azure DevOps Pipelines
- Docker
- Docker Compose

## Legacy Technologies

- ASP.NET Core MVC
- Razor Views
- jQuery
- AJAX

---

# System Architecture

DevFlow follows a layered Clean Architecture approach.

```text
                         ┌──────────────────────┐
                         │     Angular 17+      │
                         │       Frontend       │
                         └───────────┬──────────┘
                                     │
                                  HTTPS
                                     │
                                     ▼
                         ┌──────────────────────┐
                         │   ASP.NET Core API   │
                         │ Controllers / Hubs   │
                         └───────────┬──────────┘
                                     │
                                     ▼
                         ┌──────────────────────┐
                         │ Application Layer    │
                         │ Use Cases / Services │
                         │ Validation           │
                         └───────────┬──────────┘
                                     │
                                     ▼
                         ┌──────────────────────┐
                         │    Domain Layer      │
                         │ Entities             │
                         │ Business Rules       │
                         └──────────────────────┘

                                     ▲
                                     │

                         ┌──────────────────────┐
                         │ Infrastructure Layer │
                         │ EF Core              │
                         │ SQL Server           │
                         │ Azure Storage        │
                         │ Email Services       │
                         │ Caching              │
                         └──────────────────────┘
```

---

# Solution Structure

```text
DevFlow/
│
├── backend/
│   │
│   ├── src/
│   │   │
│   │   ├── DevFlow.API/
│   │   │
│   │   ├── DevFlow.Application/
│   │   │
│   │   ├── DevFlow.Domain/
│   │   │
│   │   └── DevFlow.Infrastructure/
│   │
│   └── tests/
│       │
│       ├── DevFlow.UnitTests/
│       │
│       └── DevFlow.IntegrationTests/
│
├── frontend/
│   │
│   └── devflow-angular/
│
├── legacy-admin/
│   │
│   └── DevFlow.Admin/
│
├── docs/
│   │
│   ├── architecture/
│   ├── database/
│   └── api/
│
├── .github/
│   │
│   └── workflows/
│
├── docker-compose.yml
│
├── .gitignore
│
├── LICENSE
│
└── README.md
```

---

# Clean Architecture

The backend follows Clean Architecture principles.

## Domain Layer

Contains the core business logic.

Responsibilities:

- Domain entities
- Value objects
- Enums
- Domain exceptions
- Core business rules

The Domain layer does not depend on any other project.

## Application Layer

Contains application-specific business logic.

Responsibilities:

- Use cases
- DTOs
- Interfaces
- Validation
- Service abstractions
- Commands and queries

The Application layer depends only on the Domain layer.

## Infrastructure Layer

Contains implementations of external services.

Responsibilities:

- Entity Framework Core
- SQL Server
- Repository implementations
- Azure Blob Storage
- Email services
- Redis
- External APIs

The Infrastructure layer depends on the Application and Domain layers.

## API Layer

The entry point of the backend application.

Responsibilities:

- Controllers
- Middleware
- Dependency injection configuration
- Authentication configuration
- Swagger configuration
- SignalR hubs
- HTTP request and response handling

---

# Core Modules

DevFlow consists of the following modules:

```text
Authentication
      │
      ▼
Organization Management
      │
      ▼
Team Management
      │
      ▼
Project Management
      │
      ▼
Issue Management
      │
      ├──────────────┐
      ▼              ▼
Sprint Management   Kanban Board
      │              │
      └──────┬───────┘
             ▼
       Collaboration
             │
             ▼
        Notifications
             │
             ▼
     Dashboard & Analytics
```

---

# User Roles

DevFlow initially supports three application roles.

## Admin

Responsibilities:

- Manage organizations
- Manage organization members
- Manage teams
- Manage projects
- Access administrative reports
- View audit logs

## Manager

Responsibilities:

- Manage projects
- Manage project members
- Create and manage sprints
- Create and assign issues
- Monitor project progress

## Developer

Responsibilities:

- View assigned projects
- View issues
- Update assigned issues
- Change issue status
- Add comments
- Upload attachments

Authorization is enforced on both the frontend and backend.

Backend authorization remains the primary security mechanism.

---

# Issue Workflow

The default issue lifecycle is:

```text
Backlog
   │
   ▼
To Do
   │
   ▼
In Progress
   │
   ▼
In Review
   │
   ▼
Done
```

Every status change is recorded in the activity history.

---

# Database Design

The primary entities include:

```text
User

Organization
OrganizationMember

Team
TeamMember

Project
ProjectMember

Issue
IssueComment

Sprint

Attachment

Notification

ActivityLog

AuditLog
```

Example relationships:

```text
Organization
    │
    ├── OrganizationMembers
    │
    ├── Teams
    │
    └── Projects

Project
    │
    ├── ProjectMembers
    │
    ├── Issues
    │
    └── Sprints

Issue
    │
    ├── Comments
    │
    ├── Attachments
    │
    └── ActivityLogs
```

A complete Entity Relationship Diagram will be maintained inside:

```text
docs/database/
```

---

# API Design

DevFlow exposes RESTful APIs.

Example endpoints:

## Authentication

```text
POST /api/auth/register

POST /api/auth/login

POST /api/auth/refresh-token
```

## Organizations

```text
GET    /api/organizations

GET    /api/organizations/{id}

POST   /api/organizations

PUT    /api/organizations/{id}

DELETE /api/organizations/{id}
```

## Teams

```text
GET    /api/organizations/{organizationId}/teams

POST   /api/organizations/{organizationId}/teams

PUT    /api/teams/{id}

DELETE /api/teams/{id}
```

## Projects

```text
GET    /api/projects

GET    /api/projects/{id}

POST   /api/projects

PUT    /api/projects/{id}

DELETE /api/projects/{id}
```

## Issues

```text
GET    /api/projects/{projectId}/issues

GET    /api/issues/{id}

POST   /api/projects/{projectId}/issues

PUT    /api/issues/{id}

DELETE /api/issues/{id}
```

## Comments

```text
GET  /api/issues/{issueId}/comments

POST /api/issues/{issueId}/comments
```

## Sprints

```text
GET  /api/projects/{projectId}/sprints

POST /api/projects/{projectId}/sprints

POST /api/sprints/{id}/start

POST /api/sprints/{id}/complete
```

---

# Authentication and Authorization

DevFlow uses JWT-based authentication.

Authentication flow:

```text
User
  │
  ▼
Login Request
  │
  ▼
Validate Credentials
  │
  ▼
Generate JWT
  │
  ▼
Return Access Token
  │
  ▼
Angular Stores Authentication State
  │
  ▼
HTTP Interceptor Adds Token
  │
  ▼
Backend Validates JWT
  │
  ▼
Authorization Policies Applied
```

Security considerations include:

- Secure password hashing
- JWT expiration
- Role-based authorization
- Resource-based authorization
- Organization-level data isolation
- Secure secret management

---

# Real-Time Communication

SignalR will provide real-time communication between the backend and frontend.

Use cases include:

- Real-time issue status updates
- New issue notifications
- Real-time comments
- Assignment notifications
- Kanban board synchronization

---

# Third-Party Integrations

DevFlow will integrate with external services.

## Azure Blob Storage

Used for:

- Issue attachments
- Project documents
- File storage

## Email Service

Used for:

- User invitations
- Issue assignment notifications
- Sprint notifications

## GitHub Integration

Potential capabilities include:

- Link repositories to projects
- Display repository information
- Associate pull requests with issues

---

# Legacy Admin Module

To demonstrate experience with legacy enterprise technologies, DevFlow includes a separate administrative reporting module.

Technology stack:

```text
ASP.NET Core MVC

Razor Views

jQuery

AJAX
```

Features include:

- User statistics
- Organization statistics
- Project reports
- Issue reports
- System activity reports

The jQuery module communicates with backend APIs using AJAX.

---

# Testing Strategy

DevFlow follows a multi-level testing strategy.

## Unit Testing

Used for:

- Business logic
- Application services
- Validation
- Authorization rules
- Sprint lifecycle logic

## Integration Testing

Used for:

- Authentication APIs
- Organization APIs
- Project APIs
- Issue APIs
- Database operations
- Authorization

## Frontend Testing

Used for:

- Angular components
- Angular services
- Route guards
- Forms

---

# Performance Optimization

Performance techniques include:

- Asynchronous API operations
- Server-side pagination
- Database indexing
- Optimized Entity Framework queries
- Projection using DTOs
- Redis caching
- In-memory caching
- Response compression

Potential performance issues such as N+1 queries will be monitored and optimized.

---

# Security

Security practices include:

- JWT authentication
- Secure password hashing
- Role-based authorization
- Resource-based authorization
- Input validation
- HTTPS
- Rate limiting
- Secure HTTP headers
- Secret management
- File upload validation
- SQL injection protection through EF Core
- Cross-Origin Resource Sharing configuration

---

# Error Handling and Logging

DevFlow uses centralized error handling.

Features include:

- Global exception middleware
- Standardized API error responses
- Structured logging
- Request tracing
- Health checks

Example error response:

```json
{
  "status": 404,
  "title": "Resource Not Found",
  "detail": "The requested project could not be found.",
  "traceId": "request-trace-id"
}
```

---

# CI/CD Pipeline

The CI/CD workflow automates application testing, building, and deployment.

```text
Developer Push
      │
      ▼
Pull Request
      │
      ▼
Restore Dependencies
      │
      ▼
Build ASP.NET Core
      │
      ▼
Run Backend Tests
      │
      ▼
Install Angular Dependencies
      │
      ▼
Build Angular Application
      │
      ▼
Run Frontend Tests
      │
      ▼
Build Docker Images
      │
      ▼
Deploy Application
      │
      ▼
Azure
```

---

# Azure Deployment

The planned production infrastructure is:

```text
Angular Frontend
        │
        ▼
Azure Static Web Apps
        │
        ▼
ASP.NET Core API
        │
        ▼
Azure App Service
        │
        ├──────────────┐
        ▼              ▼
    Azure SQL     Azure Blob Storage
        │
        ▼
Application Insights
```

Azure services used:

- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Azure Application Insights

---



---

# Getting Started

## Prerequisites

Install the following tools:

- .NET SDK
- Node.js
- Angular CLI
- Microsoft SQL Server
- Git
- Docker Desktop
- Visual Studio or Visual Studio Code

---

# Clone the Repository

```bash
git clone <repository-url>

cd DevFlow
```

---

# Backend Setup

Navigate to the backend directory:

```bash
cd backend
```

Restore dependencies:

```bash
dotnet restore
```

Configure the database connection.

Apply Entity Framework migrations:

```bash
dotnet ef database update
```

Run the backend:

```bash
dotnet run --project src/DevFlow.API
```

---

# Frontend Setup

Navigate to the Angular application:

```bash
cd frontend/devflow-angular
```

Install dependencies:

```bash
npm install
```

Run the development server:

```bash
ng serve
```

The frontend will run using the URL displayed by the Angular CLI.

---

# Environment Configuration

Sensitive configuration values should never be committed to source control.

Example backend configuration:

```text
ConnectionStrings__DefaultConnection

Jwt__Key

Jwt__Issuer

Jwt__Audience

AzureStorage__ConnectionString

Email__ApiKey

Redis__ConnectionString
```

Use environment variables, development secrets, or Azure configuration services to manage sensitive values.

---

# Running with Docker

After Docker support is implemented:

```bash
docker compose up --build
```

The Docker Compose environment will contain the required application services for local development.

---

# API Documentation

Swagger/OpenAPI documentation will be available when the backend application is running.

The exact local URL depends on the API launch configuration.

Swagger provides:

- API endpoint documentation
- Request schemas
- Response schemas
- Authentication testing
- Interactive API execution

---

# Project Status

**Current Status:** Planning and Design

Current development phase:

```text
Phase 1 — Requirements and Planning
```

The project is under active development.

---

# Future Improvements

Potential future improvements include:

- OAuth authentication
- Microsoft authentication
- GitHub authentication
- Refresh token rotation
- Custom project workflows
- Webhooks
- Advanced analytics
- Sprint velocity charts
- Burndown charts
- Issue dependencies
- Project templates
- Dark mode
- Mobile responsive improvements
- Kubernetes deployment
- Microservice experimentation

---

# Contributing

Contributions, issues, and feature requests are welcome.

Development workflow:

```text
Create Issue
    ↓
Create Feature Branch
    ↓
Implement Changes
    ↓
Write Tests
    ↓
Create Pull Request
    ↓
Code Review
    ↓
Merge
```

Example branch naming conventions:

```text
feature/user-authentication

feature/organization-management

feature/issue-management

fix/login-validation

docs/update-architecture
```

Example commit conventions:

```text
feat: add user registration endpoint

feat: implement issue assignment

fix: resolve JWT expiration validation

test: add organization integration tests

docs: update project architecture
```

---

# License

This project is licensed under the MIT License.

---

# Author

**Nagbhushan Pai**

Software Engineer | Full-Stack Development | Machine Learning

---

# Acknowledgements

DevFlow is inspired by modern project management and software development platforms such as Jira and Azure DevOps.

The project is being developed as a practical demonstration of enterprise full-stack software engineering using the .NET and Angular ecosystem.
