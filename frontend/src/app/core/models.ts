// DTOs returned by the DevFlow API plus display helpers for its numeric enums.

export enum IssueType { Task = 1, Bug = 2, Story = 3 }
export enum IssueStatus { Backlog = 1, Todo = 2, InProgress = 3, InReview = 4, Done = 5, Cancelled = 6 }
export enum IssuePriority { Low = 1, Medium = 2, High = 3, Critical = 4 }
export enum ProjectStatus { Planning = 1, Active = 2, OnHold = 3, Completed = 4, Archived = 5 }
export enum SprintStatus { Planned = 1, Active = 2, Completed = 3, Cancelled = 4 }
/** Lower value = more privileges. */
export enum ProjectRole { Manager = 1, Developer = 2, Viewer = 3 }

export type OrganizationRole = 'Owner' | 'Admin' | 'Member';
export type TeamRole = 'Lead' | 'Developer' | 'Viewer';

export interface Option<T> { value: T; label: string; }

export const ISSUE_TYPES: Option<IssueType>[] = [
  { value: IssueType.Task, label: 'Task' },
  { value: IssueType.Bug, label: 'Bug' },
  { value: IssueType.Story, label: 'Story' },
];

export const ISSUE_STATUSES: Option<IssueStatus>[] = [
  { value: IssueStatus.Backlog, label: 'Backlog' },
  { value: IssueStatus.Todo, label: 'To do' },
  { value: IssueStatus.InProgress, label: 'In progress' },
  { value: IssueStatus.InReview, label: 'In review' },
  { value: IssueStatus.Done, label: 'Done' },
  { value: IssueStatus.Cancelled, label: 'Cancelled' },
];

export const ISSUE_PRIORITIES: Option<IssuePriority>[] = [
  { value: IssuePriority.Low, label: 'Low' },
  { value: IssuePriority.Medium, label: 'Medium' },
  { value: IssuePriority.High, label: 'High' },
  { value: IssuePriority.Critical, label: 'Critical' },
];

export const PROJECT_STATUSES: Option<ProjectStatus>[] = [
  { value: ProjectStatus.Planning, label: 'Planning' },
  { value: ProjectStatus.Active, label: 'Active' },
  { value: ProjectStatus.OnHold, label: 'On hold' },
  { value: ProjectStatus.Completed, label: 'Completed' },
  { value: ProjectStatus.Archived, label: 'Archived' },
];

export const SPRINT_STATUSES: Option<SprintStatus>[] = [
  { value: SprintStatus.Planned, label: 'Planned' },
  { value: SprintStatus.Active, label: 'Active' },
  { value: SprintStatus.Completed, label: 'Completed' },
  { value: SprintStatus.Cancelled, label: 'Cancelled' },
];

export const PROJECT_ROLES: Option<ProjectRole>[] = [
  { value: ProjectRole.Manager, label: 'Manager' },
  { value: ProjectRole.Developer, label: 'Developer' },
  { value: ProjectRole.Viewer, label: 'Viewer' },
];

export function labelOf<T>(options: Option<T>[], value: T | null | undefined): string {
  return options.find(option => option.value === value)?.label ?? 'Unknown';
}

export interface AuthResponse {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
  token: string;
  expiresAtUtc: string;
}

export interface UserLookup { id: string; firstName: string; lastName: string; email: string; }

export interface Organization { id: string; name: string; description?: string | null; ownerId: string; myRole?: OrganizationRole | null; }
export interface OrganizationMember { userId: string; role: OrganizationRole; fullName: string; email: string; }
export interface Team { id: string; organizationId: string; name: string; description?: string | null; }
export interface TeamMember { userId: string; role: TeamRole; fullName: string; email: string; }

export interface Project {
  id: string;
  organizationId: string;
  name: string;
  key: string;
  description?: string | null;
  status: ProjectStatus;
  ownerId: string;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  gitHubRepositoryOwner?: string | null;
  gitHubRepositoryName?: string | null;
  myRole?: ProjectRole | null;
}

export interface ProjectMember { userId: string; role: ProjectRole; fullName: string; email: string; }

export interface Page<T> { items: T[]; page: number; pageSize: number; totalCount: number; }

export interface Issue {
  id: string;
  projectId: string;
  issueNumber: number;
  title: string;
  description?: string | null;
  type: IssueType;
  status: IssueStatus;
  priority: IssuePriority;
  reporterId: string;
  assigneeId?: string | null;
  sprintId?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface Comment { id: string; issueId: string; authorId: string; content: string; createdAtUtc: string; updatedAtUtc?: string | null; }
export interface HistoryEntry { id: string; issueId: string; fieldName: string; oldValue?: string | null; newValue?: string | null; changedById: string; createdAtUtc: string; }
export interface Attachment { id: string; issueId: string; fileName: string; downloadUrl: string; contentType: string; fileSize: number; uploadedById: string; createdAtUtc: string; }

export interface Sprint {
  id: string;
  projectId: string;
  name: string;
  goal?: string | null;
  startDateUtc?: string | null;
  endDateUtc?: string | null;
  status: SprintStatus;
  issueCount: number;
}

export interface BacklogIssue { id: string; issueNumber: number; title: string; status: IssueStatus; priority: IssuePriority; sprintId?: string | null; }

export interface NotificationItem { id: string; title: string; message: string; isRead: boolean; createdAtUtc: string; }

export interface GitHubSummary { owner: string; name: string; description?: string | null; openIssuesCount: number; defaultBranch: string; htmlUrl: string; }

export const ATTACHMENT_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.pdf', '.txt', '.log', '.md', '.csv', '.json', '.zip', '.docx', '.xlsx', '.pptx'];
export const MAX_ATTACHMENT_BYTES = 25 * 1024 * 1024;
