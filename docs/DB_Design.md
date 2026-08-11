# Database Design (Phase 2)

Entities: User, Organization, OrganizationMember, Team, TeamMember, Project, ProjectTeam, Issue

Constraints:
- PK: all Id as UNIQUEIDENTIFIER (Guid)
- FK constraints for all relations
- User.Email unique
- Organization.Slug unique per org
- Project.Name + OrgId unique

Indexes:
- IX_User_Email (unique)
- IX_Organization_Slug (unique)
- IX_Project_OrgId_Name (nonclustered unique)
- IX_Issue_ProjectId_Status_Priority (for filtering)
- IX_Team_OrgId (for lookup)

Notes:
- Soft-delete via IsDeleted boolean on major entities
- Use schema dbo
