import { OrganizationRole, ProjectRole } from './models';

/** What the current user may do in a project. Mirrors the API's rules so the UI only offers allowed actions. */
export interface ProjectPermissions {
  readonly role: ProjectRole | null;
  readonly canContribute: boolean;
  readonly canManage: boolean;
  canDeleteIssue(reporterId: string, userId: string | undefined): boolean;
  canDeleteOwned(ownerId: string, userId: string | undefined): boolean;
}

export function projectPermissions(role: ProjectRole | null | undefined): ProjectPermissions {
  const effective = role ?? null;
  const canManage = effective === ProjectRole.Manager;
  const canContribute = effective === ProjectRole.Manager || effective === ProjectRole.Developer;

  return {
    role: effective,
    canContribute,
    canManage,
    canDeleteIssue: (reporterId, userId) => canManage || (canContribute && reporterId === userId),
    canDeleteOwned: (ownerId, userId) => canManage || (canContribute && ownerId === userId),
  };
}

export function canManageOrganization(role: OrganizationRole | null | undefined): boolean {
  return role === 'Owner' || role === 'Admin';
}

export function isOrganizationOwner(role: OrganizationRole | null | undefined): boolean {
  return role === 'Owner';
}
