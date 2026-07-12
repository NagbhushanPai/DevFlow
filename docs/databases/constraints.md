# DevFlow — Database Constraints

## 1. Overview

This document defines the initial database and application constraints for DevFlow.

Constraints protect:

* Data integrity.
* Uniqueness.
* Referential integrity.
* Valid entity states.
* Business rules.

Some constraints shall be enforced directly by SQL Server.

Others shall be enforced by the Domain and Application layers.

---

# 2. User Constraints

```text
Field               Constraint

Id                  Primary Key
Email               Required
NormalizedEmail     Required
PasswordHash        Required
FirstName           Required
LastName            Required
IsActive            Required
CreatedAt           Required
```

Recommended maximum lengths:

```text
Email               320
NormalizedEmail     320
FirstName           100
LastName            100
```

Uniqueness:

```text
NormalizedEmail must be unique.
```

---

# 3. Organization Constraints

```text
Field               Constraint

Id                  Primary Key
Name                Required
Slug                Required
IsActive            Required
CreatedAt           Required
```

Maximum lengths:

```text
Name                200
Slug                100
Description         2000
```

Uniqueness:

```text
Organization.Slug must be unique.
```

Slug values should be normalized before persistence.

Example:

```text
DevFlow Engineering

becomes

devflow-engineering
```

---

# 4. OrganizationMember Constraints

```text
Field               Constraint

Id                  Primary Key
OrganizationId      Required
UserId              Required
Role                Required
JoinedAt            Required
IsActive            Required
```

Composite uniqueness:

```text
OrganizationId + UserId
```

A User cannot have duplicate membership records for the same Organization.

---

# 5. Team Constraints

```text
Field               Constraint

Id                  Primary Key
OrganizationId      Required
Name                Required
IsActive            Required
CreatedAt           Required
```

Maximum lengths:

```text
Name                150
Description         2000
```

Composite uniqueness:

```text
OrganizationId + Name
```

Two Teams within the same Organization cannot use the same normalized name.

---

# 6. TeamMember Constraints

Required fields:

```text
Id
TeamId
UserId
JoinedAt
IsActive
```

Composite uniqueness:

```text
TeamId + UserId
```

A User cannot have duplicate membership records for the same Team.

Application logic must verify that the User belongs to the Team's Organization.

---

# 7. Project Constraints

Required fields:

```text
Id
OrganizationId
Name
Key
Status
IsArchived
CreatedAt
```

Maximum lengths:

```text
Name                200
Key                 10
Description         5000
```

Project Key requirements:

* Must be normalized to uppercase.
* Should contain only supported alphanumeric characters.
* Must be unique within an Organization.

Composite uniqueness:

```text
OrganizationId + Key
```

Examples:

```text
DEV
AUTH
PAY
WEB
```

Project dates must satisfy:

```text
TargetEndDate >= StartDate
```

when both dates are provided.

---

# 8. ProjectMember Constraints

Required fields:

```text
Id
ProjectId
UserId
Role
JoinedAt
IsActive
```

Composite uniqueness:

```text
ProjectId + UserId
```

A User cannot have duplicate membership records for the same Project.

The User must belong to the Project's Organization.

---

# 9. ProjectTeam Constraints

Required fields:

```text
Id
ProjectId
TeamId
AssignedAt
```

Composite uniqueness:

```text
ProjectId + TeamId
```

The Team and Project must belong to the same Organization.

---

# 10. Issue Constraints

Required fields:

```text
Id
ProjectId
ReporterId
IssueNumber
Title
Type
Status
Priority
CreatedAt
```

Maximum lengths:

```text
Title               300
Description         10000
```

Composite uniqueness:

```text
ProjectId + IssueNumber
```

IssueNumber must be:

```text
IssueNumber > 0
```

Issue DueDate may be null.

The Reporter must have access to the Project.

The Assignee, when provided, must have access to the Project.

The Sprint, when provided, must belong to the same Project as the Issue.

---

# 11. Comment Constraints

Required fields:

```text
Id
IssueId
AuthorId
Content
CreatedAt
IsDeleted
```

Maximum length:

```text
Content             10000
```

Comment Content:

* Cannot be null.
* Cannot be empty.
* Cannot contain only whitespace.

Deleted comments should not expose their original Content through standard API responses.

---

# 12. ActivityHistory Constraints

Required fields:

```text
Id
IssueId
ActionType
CreatedAt
```

Optional fields:

```text
ActorId
FieldName
OldValue
NewValue
```

Maximum lengths:

```text
FieldName           100
OldValue            4000
NewValue            4000
```

Activity history records should be immutable after creation.

---

# 13. Sprint Constraints

Required fields:

```text
Id
ProjectId
Name
Status
CreatedAt
```

Maximum lengths:

```text
Name                200
Goal                2000
```

Sprint date validation:

```text
EndDate >= StartDate
```

when both dates are provided.

A Project should have no more than one Active Sprint.

This rule shall initially be enforced by application logic.

---

# 14. Enum Constraints

Enums shall use explicit numeric values.

Example:

```text
IssueStatus

Backlog      = 1
Todo         = 2
InProgress   = 3
InReview     = 4
Done         = 5
```

Explicit values prevent accidental database changes when enum declaration order changes.

The same approach shall be used for all persisted enums.

---

# 15. Domain Constraints

The following rules shall be enforced primarily through the Domain or Application layer:

* Organization must always have at least one Owner.
* Team members must belong to the Organization.
* Project members must belong to the Organization.
* Project Teams must belong to the same Organization as the Project.
* Issue assignees must have Project access.
* Sprint and Issue must belong to the same Project.
* Only authorized users may modify protected resources.
* Completed Sprints cannot be restarted without an explicitly defined future business rule.
* Archived Projects cannot accept normal Issue modifications.
* Activity history records cannot be modified.

---

# 16. Concurrency Constraints

Issue numbers must be generated safely during concurrent Issue creation.

The Project entity shall contain:

```text
NextIssueNumber : int
```

Initial value:

```text
1
```

Issue creation shall:

1. Retrieve the Project.
2. Reserve the current NextIssueNumber.
3. Increment NextIssueNumber.
4. Create the Issue.
5. Save the operation transactionally.

Optimistic concurrency protection shall be introduced where required.

The Project entity may contain:

```text
RowVersion : byte[]
```

configured as a SQL Server row version.

---

# 17. Validation Responsibility

Validation shall be divided between layers.

## Database

Responsible for:

* Primary keys.
* Foreign keys.
* Unique constraints.
* Required columns.
* Maximum lengths where configured.
* Referential integrity.

## Domain Layer

Responsible for:

* Valid entity state.
* Business invariants.
* State transitions.

## Application Layer

Responsible for:

* Request validation.
* Cross-entity validation.
* Authorization-dependent business rules.
* Resource existence checks.

## API Layer

Responsible for:

* HTTP model binding.
* Authentication enforcement.
* Returning appropriate HTTP responses.
