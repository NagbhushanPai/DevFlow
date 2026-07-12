# DevFlow — Database Relationships

## 1. Overview

This document defines the relationships between DevFlow database entities.

The schema uses explicit join entities for many-to-many relationships.

---

# 2. User Relationships

A User can:

* Belong to multiple Organizations.
* Belong to multiple Teams.
* Belong to multiple Projects.
* Report multiple Issues.
* Be assigned multiple Issues.
* Create multiple Comments.
* Perform multiple Activities.

Relationships:

```text
User 1 --- * OrganizationMember
User 1 --- * TeamMember
User 1 --- * ProjectMember
User 1 --- * Issue (Reporter)
User 1 --- * Issue (Assignee)
User 1 --- * Comment
User 1 --- * ActivityHistory
```

---

# 3. Organization Relationships

An Organization:

* Has many OrganizationMembers.
* Has many Teams.
* Has many Projects.

Relationships:

```text
Organization 1 --- * OrganizationMember
Organization 1 --- * Team
Organization 1 --- * Project
```

Deleting an Organization must not automatically hard-delete all related business data.

Organization deletion shall initially be implemented as deactivation or archival.

---

# 4. OrganizationMember Relationships

OrganizationMember connects:

```text
User <-> Organization
```

Relationships:

```text
Organization 1 --- * OrganizationMember
User         1 --- * OrganizationMember
```

Together this represents:

```text
User * --- * Organization
```

A User can belong to multiple Organizations.

An Organization can contain multiple Users.

---

# 5. Team Relationships

A Team:

* Belongs to one Organization.
* Has many TeamMembers.
* Can be assigned to many Projects.

Relationships:

```text
Organization 1 --- * Team
Team         1 --- * TeamMember
Team         1 --- * ProjectTeam
```

---

# 6. TeamMember Relationships

TeamMember connects:

```text
User <-> Team
```

Relationships:

```text
Team 1 --- * TeamMember
User 1 --- * TeamMember
```

Together this represents:

```text
User * --- * Team
```

A User must belong to the Team's Organization before becoming a TeamMember.

This rule shall be enforced by application logic.

---

# 7. Project Relationships

A Project:

* Belongs to one Organization.
* Has many ProjectMembers.
* Can have many assigned Teams.
* Contains many Issues.
* Contains many Sprints.

Relationships:

```text
Organization 1 --- * Project

Project 1 --- * ProjectMember
Project 1 --- * ProjectTeam
Project 1 --- * Issue
Project 1 --- * Sprint
```

---

# 8. ProjectMember Relationships

ProjectMember connects:

```text
User <-> Project
```

Relationships:

```text
Project 1 --- * ProjectMember
User    1 --- * ProjectMember
```

Together this represents:

```text
User * --- * Project
```

A User must belong to the Project's Organization before becoming a ProjectMember.

---

# 9. ProjectTeam Relationships

ProjectTeam connects:

```text
Project <-> Team
```

Relationships:

```text
Project 1 --- * ProjectTeam
Team    1 --- * ProjectTeam
```

Together this represents:

```text
Project * --- * Team
```

A Team and Project must belong to the same Organization.

Assigning a Team to a Project does not automatically create ProjectMember records for every TeamMember.

Team assignment and explicit project membership remain separate concepts.

---

# 10. Issue Relationships

An Issue:

* Belongs to one Project.
* Is reported by one User.
* May be assigned to one User.
* May belong to one Sprint.
* Has many Comments.
* Has many ActivityHistory records.

Relationships:

```text
Project 1 --- * Issue

User 1 --- * Issue (Reporter)

User 1 --- * Issue (Assignee)

Sprint 1 --- * Issue

Issue 1 --- * Comment

Issue 1 --- * ActivityHistory
```

The following relationships are required:

```text
ProjectId
ReporterId
```

The following relationships are optional:

```text
AssigneeId
SprintId
```

---

# 11. Comment Relationships

A Comment:

* Belongs to one Issue.
* Is created by one User.

Relationships:

```text
Issue 1 --- * Comment
User  1 --- * Comment
```

Comments shall support soft deletion.

Deleting a User shall not automatically delete their Comments.

---

# 12. ActivityHistory Relationships

ActivityHistory:

* Belongs to one Issue.
* May reference one User as the Actor.

Relationships:

```text
Issue 1 --- * ActivityHistory

User 1 --- * ActivityHistory
```

ActorId is nullable because activities may eventually be generated automatically by the system.

---

# 13. Sprint Relationships

A Sprint:

* Belongs to one Project.
* Contains zero or more Issues.

Relationships:

```text
Project 1 --- * Sprint

Sprint 1 --- * Issue
```

An Issue may belong to zero or one Sprint.

A Sprint may contain multiple Issues.

An Issue and its Sprint must belong to the same Project.

---

# 14. Complete Relationship Summary

```text
User
│
├── OrganizationMember ─── Organization
│
├── TeamMember ─────────── Team
│
├── ProjectMember ──────── Project
│
├── Reported Issues
│
├── Assigned Issues
│
├── Comments
│
└── ActivityHistory


Organization
│
├── OrganizationMembers
├── Teams
└── Projects


Team
│
├── TeamMembers
└── ProjectTeams ───────── Project


Project
│
├── ProjectMembers
├── ProjectTeams
├── Issues
└── Sprints


Sprint
│
└── Issues


Issue
│
├── Comments
└── ActivityHistory
```

---

# 15. Delete Behavior Strategy

DevFlow shall avoid uncontrolled cascading deletes.

Recommended delete behaviors:

```text
Relationship                         Delete Behavior

Organization -> OrganizationMember   Restrict
Organization -> Team                 Restrict
Organization -> Project              Restrict

User -> OrganizationMember           Restrict
User -> TeamMember                   Restrict
User -> ProjectMember                Restrict

Team -> TeamMember                   Cascade
Team -> ProjectTeam                  Cascade

Project -> ProjectMember             Cascade
Project -> ProjectTeam               Cascade
Project -> Issue                     Restrict
Project -> Sprint                    Restrict

Sprint -> Issue                      SetNull

Issue -> Comment                     Cascade
Issue -> ActivityHistory             Cascade

User -> Issue Reporter               Restrict
User -> Issue Assignee               SetNull
User -> Comment                      Restrict
User -> ActivityHistory Actor        SetNull
```

The exact Entity Framework Core mappings shall be implemented using Fluent API configurations.

---

# 16. Cross-Entity Business Rules

The database relationships alone cannot enforce every DevFlow business rule.

Application logic must additionally enforce:

* Team members must belong to the Team's Organization.
* Project members must belong to the Project's Organization.
* Assigned Teams must belong to the Project's Organization.
* Issue assignees must have access to the Project.
* Sprint and Issue must belong to the same Project.
* Organization Owners cannot be removed if doing so leaves the Organization without an Owner.
* Project-specific issue numbers must be generated safely.
