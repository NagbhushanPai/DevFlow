# DevFlow — Entity Relationship Diagram

## 1. Overview

This document contains the initial Entity Relationship Diagram for the DevFlow database.

The diagram uses Mermaid syntax and can be rendered by GitHub and compatible Markdown tools.

---

# 2. Entity Relationship Diagram

```mermaid
erDiagram

    USER {
        Guid Id PK
        string Email
        string NormalizedEmail
        string PasswordHash
        string FirstName
        string LastName
        bool IsActive
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }

    ORGANIZATION {
        Guid Id PK
        string Name
        string Slug
        string Description
        bool IsActive
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }

    ORGANIZATION_MEMBER {
        Guid Id PK
        Guid OrganizationId FK
        Guid UserId FK
        int Role
        DateTimeOffset JoinedAt
        bool IsActive
    }

    TEAM {
        Guid Id PK
        Guid OrganizationId FK
        string Name
        string Description
        bool IsActive
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }

    TEAM_MEMBER {
        Guid Id PK
        Guid TeamId FK
        Guid UserId FK
        DateTimeOffset JoinedAt
        bool IsActive
    }

    PROJECT {
        Guid Id PK
        Guid OrganizationId FK
        string Name
        string Key
        string Description
        int Status
        DateOnly StartDate
        DateOnly TargetEndDate
        int NextIssueNumber
        bool IsArchived
        DateTimeOffset ArchivedAt
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
        byte RowVersion
    }

    PROJECT_MEMBER {
        Guid Id PK
        Guid ProjectId FK
        Guid UserId FK
        int Role
        DateTimeOffset JoinedAt
        bool IsActive
    }

    PROJECT_TEAM {
        Guid Id PK
        Guid ProjectId FK
        Guid TeamId FK
        DateTimeOffset AssignedAt
    }

    ISSUE {
        Guid Id PK
        Guid ProjectId FK
        Guid SprintId FK
        Guid ReporterId FK
        Guid AssigneeId FK
        int IssueNumber
        string Title
        string Description
        int Type
        int Status
        int Priority
        DateOnly DueDate
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }

    COMMENT {
        Guid Id PK
        Guid IssueId FK
        Guid AuthorId FK
        string Content
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
        bool IsDeleted
        DateTimeOffset DeletedAt
    }

    ACTIVITY_HISTORY {
        Guid Id PK
        Guid IssueId FK
        Guid ActorId FK
        int ActionType
        string FieldName
        string OldValue
        string NewValue
        DateTimeOffset CreatedAt
    }

    SPRINT {
        Guid Id PK
        Guid ProjectId FK
        string Name
        string Goal
        int Status
        DateOnly StartDate
        DateOnly EndDate
        DateTimeOffset CreatedAt
        DateTimeOffset UpdatedAt
    }


    USER ||--o{ ORGANIZATION_MEMBER : belongs_to
    ORGANIZATION ||--o{ ORGANIZATION_MEMBER : contains

    ORGANIZATION ||--o{ TEAM : contains
    USER ||--o{ TEAM_MEMBER : joins
    TEAM ||--o{ TEAM_MEMBER : contains

    ORGANIZATION ||--o{ PROJECT : owns

    USER ||--o{ PROJECT_MEMBER : participates
    PROJECT ||--o{ PROJECT_MEMBER : contains

    TEAM ||--o{ PROJECT_TEAM : assigned
    PROJECT ||--o{ PROJECT_TEAM : contains

    PROJECT ||--o{ ISSUE : contains

    USER ||--o{ ISSUE : reports
    USER ||--o{ ISSUE : assigned_to

    PROJECT ||--o{ SPRINT : contains
    SPRINT ||--o{ ISSUE : contains

    ISSUE ||--o{ COMMENT : contains
    USER ||--o{ COMMENT : writes

    ISSUE ||--o{ ACTIVITY_HISTORY : generates
    USER ||--o{ ACTIVITY_HISTORY : performs
```

---

# 3. Core Ownership Hierarchy

```text
Organization
│
├── OrganizationMember
│
├── Team
│   └── TeamMember
│
└── Project
    │
    ├── ProjectMember
    │
    ├── ProjectTeam
    │
    ├── Sprint
    │   └── Issue
    │
    └── Issue
        ├── Comment
        └── ActivityHistory
```

---

# 4. Many-to-Many Relationships

DevFlow contains the following many-to-many relationships:

```text
User <-> Organization

implemented through:

OrganizationMember
```

```text
User <-> Team

implemented through:

TeamMember
```

```text
User <-> Project

implemented through:

ProjectMember
```

```text
Team <-> Project

implemented through:

ProjectTeam
```

Explicit join entities are used to support metadata, authorization, auditing, and future extensibility.

---

# 5. Important Schema Rules

The following rules must be maintained:

* OrganizationMember User and Organization combination must be unique.
* TeamMember User and Team combination must be unique.
* ProjectMember User and Project combination must be unique.
* ProjectTeam Team and Project combination must be unique.
* Project Key must be unique within an Organization.
* Issue Number must be unique within a Project.
* Team members must belong to the Team's Organization.
* Project members must belong to the Project's Organization.
* Assigned Teams and Projects must belong to the same Organization.
* Sprint and Issue must belong to the same Project.
* Issue Assignees must have Project access.
* Organizations must retain at least one Owner.
