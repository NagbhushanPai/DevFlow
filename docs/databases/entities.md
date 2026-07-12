# DevFlow — Database Entities

## 1. Overview

This document defines the initial database entities for DevFlow.

The database schema is designed to support:

* Authentication
* Organizations
* Organization membership
* Teams
* Team membership
* Projects
* Project membership
* Team assignment
* Issues
* Comments
* Activity history
* Sprints

The initial database implementation will use:

* SQL Server
* Entity Framework Core
* Code First migrations

---

# 2. Common Design Conventions

## Primary Keys

All primary entities shall use `Guid` identifiers.

Example:

```text
Id : Guid
```

Benefits include:

* IDs can be generated outside the database.
* IDs are difficult to enumerate.
* IDs work well in distributed systems.
* IDs are suitable for public API identifiers.

---

## Audit Fields

Primary entities should contain the following fields where appropriate:

```text
CreatedAt : DateTimeOffset
UpdatedAt : DateTimeOffset?
```

Entities that support soft deletion or archival may additionally contain:

```text
IsDeleted : bool
DeletedAt : DateTimeOffset?
```

or:

```text
IsArchived : bool
ArchivedAt : DateTimeOffset?
```

Soft deletion and archival should only be implemented where required by business rules.

---

# 3. User

Represents a registered DevFlow user.

## Properties

```text
User
----
Id                  Guid
Email               string
NormalizedEmail     string
PasswordHash        string
FirstName           string
LastName            string
IsActive            bool
CreatedAt           DateTimeOffset
UpdatedAt           DateTimeOffset?
```

## Responsibilities

A User can:

* Authenticate with DevFlow.
* Belong to multiple organizations.
* Belong to multiple teams.
* Participate in multiple projects.
* Be assigned issues.
* Create issues.
* Create comments.

## Important Note

Passwords must never be stored directly.

Only secure password hashes shall be persisted.

---

# 4. Organization

Represents a workspace containing teams, projects, and members.

## Properties

```text
Organization
------------
Id                  Guid
Name                string
Slug                string
Description         string?
IsActive            bool
CreatedAt           DateTimeOffset
UpdatedAt           DateTimeOffset?
```

## Responsibilities

An Organization can contain:

* Organization members.
* Teams.
* Projects.

---

# 5. OrganizationMember

Represents membership between a User and Organization.

This is a join entity containing additional membership information.

## Properties

```text
OrganizationMember
------------------
Id                  Guid
OrganizationId      Guid
UserId              Guid
Role                OrganizationRole
JoinedAt            DateTimeOffset
IsActive            bool
```

## Responsibilities

OrganizationMember determines:

* Whether a user belongs to an organization.
* The user's organization-level role.
* Whether the membership is currently active.

---

# 6. Team

Represents a team within an Organization.

## Properties

```text
Team
----
Id                  Guid
OrganizationId      Guid
Name                string
Description         string?
IsActive            bool
CreatedAt           DateTimeOffset
UpdatedAt           DateTimeOffset?
```

## Responsibilities

A Team:

* Belongs to one organization.
* Contains multiple members.
* Can be assigned to multiple projects.

---

# 7. TeamMember

Represents membership between a User and Team.

## Properties

```text
TeamMember
----------
Id                  Guid
TeamId              Guid
UserId              Guid
JoinedAt            DateTimeOffset
IsActive            bool
```

## Responsibilities

TeamMember determines whether a user belongs to a team.

A user must belong to the team's organization before joining the team.

---

# 8. Project

Represents a project managed within an Organization.

## Properties

```text
Project
-------
Id                  Guid
OrganizationId      Guid
Name                string
Key                 string
Description         string?
Status              ProjectStatus
StartDate           DateOnly?
TargetEndDate       DateOnly?
IsArchived          bool
ArchivedAt          DateTimeOffset?
CreatedAt           DateTimeOffset
UpdatedAt           DateTimeOffset?
NextIssueNumber     int
RowVersion          byte[]
```

## Responsibilities

A Project:

* Belongs to one organization.
* Contains project members.
* May have assigned teams.
* Contains issues.
* Contains sprints.

## Project Key

Every project shall have a short human-readable key.

Examples:

```text
DEV
FLOW
PAY
AUTH
```

The project key may later be used when generating human-readable issue identifiers.

Example:

```text
DEV-1
DEV-2
DEV-3
```

---

# 9. ProjectMember

Represents membership between a User and Project.

## Properties

```text
ProjectMember
-------------
Id                  Guid
ProjectId           Guid
UserId              Guid
Role                ProjectRole
JoinedAt            DateTimeOffset
IsActive            bool
```

## Responsibilities

ProjectMember determines:

* Whether a user can access the project.
* The user's project-level role.

---

# 10. ProjectTeam

Represents the assignment of a Team to a Project.

## Properties

```text
ProjectTeam
-----------
Id                  Guid
ProjectId           Guid
TeamId              Guid
AssignedAt          DateTimeOffset
```

## Responsibilities

ProjectTeam enables a many-to-many relationship between:

```text
Project <-> Team
```

A team may participate in multiple projects.

A project may contain multiple teams.

---

# 11. Issue

Represents work tracked inside a Project.

## Properties

```text
Issue
-----
Id                  Guid
ProjectId           Guid
SprintId            Guid?
ReporterId          Guid
AssigneeId          Guid?
IssueNumber         int
Title               string
Description         string?
Type                 IssueType
Status               IssueStatus
Priority             IssuePriority
DueDate              DateOnly?
CreatedAt            DateTimeOffset
UpdatedAt            DateTimeOffset?
```

## Responsibilities

An Issue:

* Belongs to one project.
* Is created by one user.
* May be assigned to one user.
* May belong to one sprint.
* Contains comments.
* Generates activity history.

---

# 12. Issue Number

Each issue shall have a project-specific sequential number.

Example:

```text
DEV-1
DEV-2
DEV-3

PAY-1
PAY-2
```

The database stores:

```text
Project.Key = DEV
Issue.IssueNumber = 42
```

The application displays:

```text
DEV-42
```

Issue numbers must be unique within a project.

---

# 13. Comment

Represents a comment added to an Issue.

## Properties

```text
Comment
-------
Id                  Guid
IssueId             Guid
AuthorId            Guid
Content             string
CreatedAt           DateTimeOffset
UpdatedAt           DateTimeOffset?
IsDeleted           bool
DeletedAt           DateTimeOffset?
```

## Responsibilities

A Comment:

* Belongs to one issue.
* Is created by one user.
* May be edited.
* May be soft deleted.

---

# 14. ActivityHistory

Represents an important change made to an Issue.

## Properties

```text
ActivityHistory
---------------
Id                  Guid
IssueId             Guid
ActorId             Guid?
ActionType          ActivityType
FieldName           string?
OldValue            string?
NewValue            string?
CreatedAt           DateTimeOffset
```

## Responsibilities

Activity history may record:

* Issue creation.
* Assignee changes.
* Status changes.
* Priority changes.
* Sprint changes.
* Issue updates.

## ActorId

`ActorId` is nullable to support future system-generated activities.

Example:

```text
ActorId = null
ActionType = SystemUpdate
```

---

# 15. Sprint

Represents an Agile sprint belonging to a Project.

## Properties

```text
Sprint
------
Id                  Guid
ProjectId           Guid
Name                string
Goal                string?
Status               SprintStatus
StartDate            DateOnly?
EndDate              DateOnly?
CreatedAt            DateTimeOffset
UpdatedAt            DateTimeOffset?
```

## Responsibilities

A Sprint:

* Belongs to one project.
* Contains multiple issues.
* Can be planned.
* Can be started.
* Can be completed.

---

# 16. Initial Enums

## OrganizationRole

```text
Owner
Admin
Member
```

---

## ProjectRole

```text
ProjectManager
Developer
```

---

## ProjectStatus

```text
Planning
Active
Completed
OnHold
```

---

## IssueType

```text
Task
Bug
Story
```

---

## IssueStatus

```text
Backlog
Todo
InProgress
InReview
Done
```

---

## IssuePriority

```text
Low
Medium
High
Critical
```

---

## SprintStatus

```text
Planned
Active
Completed
```

---

## ActivityType

```text
Created
Updated
StatusChanged
PriorityChanged
AssigneeChanged
SprintChanged
CommentAdded
CommentDeleted
SystemUpdate
```

---

# 17. Initial Entity List

The initial DevFlow database contains the following entities:

```text
User

Organization
OrganizationMember

Team
TeamMember

Project
ProjectMember
ProjectTeam

Issue
Comment
ActivityHistory

Sprint
```

Total initial entities:

```text
12
```

---

# 18. Entity Ownership Hierarchy

The primary ownership hierarchy is:

```text
Organization
│
├── OrganizationMembers
│
├── Teams
│   └── TeamMembers
│
└── Projects
    │
    ├── ProjectMembers
    │
    ├── ProjectTeams
    │
    ├── Sprints
    │
    └── Issues
        │
        ├── Comments
        │
        └── ActivityHistory
```

---

# 19. Important Design Decisions

## Use Explicit Join Entities

DevFlow shall use explicit join entities instead of hidden Entity Framework Core many-to-many tables.

Examples:

```text
OrganizationMember
TeamMember
ProjectMember
ProjectTeam
```

This enables:

* Additional metadata.
* Authorization logic.
* Audit information.
* Future extensibility.

---

## Use DateTimeOffset for Timestamps

Database timestamps shall use:

```text
DateTimeOffset
```

instead of:

```text
DateTime
```

This provides explicit timezone offset information and is better suited for distributed applications.

---

## Use DateOnly for Business Dates

Fields that represent calendar dates rather than exact timestamps shall use:

```text
DateOnly
```

Examples:

* Project StartDate.
* Project TargetEndDate.
* Issue DueDate.
* Sprint StartDate.
* Sprint EndDate.

---

## Keep Domain Entities Independent

Domain entities shall not contain:

* Entity Framework Core attributes.
* SQL Server-specific code.
* API request models.
* API response models.

Database mappings shall be configured inside:

```text
DevFlow.Infrastructure
```

using Entity Framework Core Fluent API configurations.

---

# 20. Future Entities

The following entities may be introduced in later development phases:

```text
Attachment
Notification
RefreshToken
AuditLog
GitHubIntegration
GitHubWebhookEvent
EmailNotification
```

These entities shall not be introduced until their corresponding features are implemented.
