# DevFlow — Database Index Strategy

## 1. Overview

This document defines the initial indexing strategy for the DevFlow SQL Server database.

Indexes shall be introduced based on:

* Uniqueness requirements.
* Foreign key lookups.
* Search operations.
* Filtering operations.
* Sorting operations.
* Pagination requirements.

Indexes should not be created for every database column.

Each additional index increases storage requirements and write costs.

---

# 2. User Indexes

## Unique Email Index

```text
NormalizedEmail
```

Type:

```text
UNIQUE
```

Purpose:

* Login lookup.
* Prevent duplicate accounts.

---

# 3. Organization Indexes

## Unique Slug Index

```text
Slug
```

Type:

```text
UNIQUE
```

Purpose:

* Organization lookup.
* Prevent duplicate slugs.

---

# 4. OrganizationMember Indexes

## Membership Unique Index

```text
OrganizationId + UserId
```

Type:

```text
UNIQUE
```

Purpose:

* Prevent duplicate memberships.
* Fast membership authorization checks.

## User Organization Lookup

```text
UserId
```

Purpose:

* Retrieve Organizations belonging to a User.

---

# 5. Team Indexes

## Organization Team Name Index

```text
OrganizationId + Name
```

Type:

```text
UNIQUE
```

Purpose:

* Prevent duplicate Team names within an Organization.

## Organization Lookup

```text
OrganizationId
```

Purpose:

* Retrieve Teams belonging to an Organization.

---

# 6. TeamMember Indexes

## Team Membership Unique Index

```text
TeamId + UserId
```

Type:

```text
UNIQUE
```

## User Team Lookup

```text
UserId
```

Purpose:

* Retrieve Teams belonging to a User.

---

# 7. Project Indexes

## Organization Project Key Index

```text
OrganizationId + Key
```

Type:

```text
UNIQUE
```

Purpose:

* Prevent duplicate Project Keys within an Organization.

## Organization Project Lookup

```text
OrganizationId
```

Purpose:

* Retrieve Projects belonging to an Organization.

## Project Filtering Index

Potential index:

```text
OrganizationId + Status + IsArchived
```

Purpose:

* Filter active Projects.
* Filter Projects by status.

This index should be validated against actual query patterns before optimization.

---

# 8. ProjectMember Indexes

## Project Membership Unique Index

```text
ProjectId + UserId
```

Type:

```text
UNIQUE
```

Purpose:

* Prevent duplicate memberships.
* Fast Project authorization checks.

## User Project Lookup

```text
UserId
```

Purpose:

* Retrieve Projects belonging to a User.

---

# 9. ProjectTeam Indexes

## Project Team Unique Index

```text
ProjectId + TeamId
```

Type:

```text
UNIQUE
```

## Team Project Lookup

```text
TeamId
```

Purpose:

* Retrieve Projects assigned to a Team.

---

# 10. Issue Indexes

Issues are expected to become one of the largest and most frequently queried tables.

## Issue Number Index

```text
ProjectId + IssueNumber
```

Type:

```text
UNIQUE
```

Purpose:

* Retrieve Issues using identifiers such as DEV-42.

---

## Project Status Index

```text
ProjectId + Status
```

Purpose:

* Kanban queries.
* Issue status filtering.

---

## Project Assignee Index

```text
ProjectId + AssigneeId
```

Purpose:

* Retrieve Issues assigned to a User.

---

## Project Priority Index

```text
ProjectId + Priority
```

Purpose:

* Filter Issues by priority.

---

## Sprint Issue Index

```text
SprintId
```

Purpose:

* Retrieve Issues belonging to a Sprint.

---

## Issue Creation Index

Potential index:

```text
ProjectId + CreatedAt
```

Purpose:

* Sort Issues by creation date.
* Cursor or offset pagination.

---

# 11. Comment Indexes

## Issue Comment Index

```text
IssueId + CreatedAt
```

Purpose:

* Retrieve Issue Comments chronologically.

---

## Author Comment Index

Potential index:

```text
AuthorId
```

Purpose:

* Administrative queries involving User activity.

This index should only be introduced when required.

---

# 12. ActivityHistory Indexes

## Issue Activity Index

```text
IssueId + CreatedAt
```

Purpose:

* Retrieve Issue history chronologically.

---

## Actor Activity Index

Potential index:

```text
ActorId
```

Purpose:

* Administrative audit queries.

---

# 13. Sprint Indexes

## Project Sprint Index

```text
ProjectId + Status
```

Purpose:

* Retrieve Sprints belonging to a Project.
* Locate Active Sprints.

---

# 14. Search Strategy

Initial search functionality shall use SQL Server-compatible queries.

Examples include:

```text
Project.Name contains search term

Issue.Title contains search term
```

Initial search shall prioritize correctness and simplicity.

Full-text search shall not be introduced during MVP development.

If search performance becomes a measurable issue, future options include:

* SQL Server Full-Text Search.
* Dedicated search infrastructure.
* External search services.

---

# 15. Pagination Strategy

Initial API development may use offset pagination.

Example:

```text
PageNumber
PageSize
```

Entity Framework Core implementation:

```text
Skip(...)
Take(...)
```

All paginated queries must use deterministic ordering.

Example:

```text
OrderBy(CreatedAt)
ThenBy(Id)
```

Cursor-based pagination may be introduced later if required.

---

# 16. Index Review Strategy

Indexes shall be reviewed after:

* Core API queries are implemented.
* Integration testing is operational.
* Representative test data exists.
* Query performance can be measured.

Indexes must solve actual query patterns rather than hypothetical performance problems.
