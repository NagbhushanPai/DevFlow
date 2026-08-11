# ER Diagram (Mermaid)

```mermaid
erDiagram
    APPLICATION_USER {
        Guid Id PK
        string FirstName
        string LastName
        string Email
    }

    ORGANIZATION {
        Guid Id PK
        string Name
        Guid OwnerId FK
    }

    ORGANIZATION_MEMBER {
        Guid Id PK
        Guid OrganizationId FK
        Guid UserId FK
        string Role
    }

    TEAM {
        Guid Id PK
        string Name
        Guid OrganizationId FK
    }

    TEAM_MEMBER {
        Guid Id PK
        Guid TeamId FK
        Guid UserId FK
        string Role
    }

    PROJECT {
        Guid Id PK
        string Name
        string Key
        Guid OwnerId FK
        Guid? TeamId FK
    }

    ISSUE {
        Guid Id PK
        string Title
        Guid ProjectId FK
    }

    APPLICATION_USER ||--o{ ORGANIZATION_MEMBER : "has"
    ORGANIZATION ||--o{ ORGANIZATION_MEMBER : "members"
    ORGANIZATION ||--o{ TEAM : "owns"
    TEAM ||--o{ TEAM_MEMBER : "members"
    APPLICATION_USER ||--o{ TEAM_MEMBER : "belongs"
    PROJECT ||--o{ ISSUE : "contains"
    TEAM ||--o{ PROJECT : "assigned"
    APPLICATION_USER ||--o{ PROJECT : "members"
```
