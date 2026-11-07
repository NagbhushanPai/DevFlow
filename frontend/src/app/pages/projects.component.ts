import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

interface Project { id: string; organizationId: string; name: string; key: string; status: number; gitHubRepositoryOwner?: string; gitHubRepositoryName?: string; }
interface Organization { id: string; name: string; }
interface Issue { id: string; issueNumber: number; title: string; status: number; priority: number; }
interface Comment { id: string; content: string; authorId: string; createdAtUtc: string; }
interface History { id: string; fieldName: string; oldValue?: string; newValue?: string; createdAtUtc: string; }
interface Attachment { id: string; fileName: string; fileUrl: string; contentType: string; fileSize: number; createdAtUtc: string; }
interface GitHubSummary { owner: string; name: string; description?: string; openIssuesCount: number; defaultBranch: string; htmlUrl: string; }

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `<section><div class="page-heading"><div><p class="eyebrow">Delivery workspace</p><h1>Projects</h1><p>Choose a project to manage issues, sprints, and integrations.</p></div><button (click)="showCreate = !showCreate">{{ showCreate ? 'Close' : '+ Add project' }}</button></div>
  <article class="card create-panel" *ngIf="showCreate"><h2>Create a project</h2><div class="form-grid"><label>Organization<select [(ngModel)]="newProject.organizationId"><option value="">Choose an organization</option><option *ngFor="let organization of organizations" [value]="organization.id">{{ organization.name }}</option></select></label><label>Project name<input [(ngModel)]="newProject.name" placeholder="e.g. Mobile app"></label><label>Key<input [(ngModel)]="newProject.key" placeholder="e.g. MOB" maxlength="10"></label><label>Description<input [(ngModel)]="newProject.description" placeholder="What is this project about?"></label></div><p *ngIf="projectError" class="error">{{ projectError }}</p><div class="toolbar"><button (click)="createProject()" [disabled]="creating">{{ creating ? 'Creating…' : 'Create project' }}</button><button class="secondary" (click)="showCreate = false">Cancel</button></div></article>
  <div class="toolbar"><input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search projects"><button (click)="load()">Search</button></div><div class="card-grid"><article class="card" *ngFor="let project of projects"><h2>{{ project.key }} · {{ project.name }}</h2><p>Status: {{ project.status }}</p><div class="card-actions"><button (click)="select(project)">View issues</button><a class="button secondary" [routerLink]="['/projects', project.id, 'board']">Board</a><a class="button secondary" [routerLink]="['/projects', project.id, 'sprints']">Sprints</a></div></article></div><p class="empty-state" *ngIf="!projects.length">No projects found. <button class="link" (click)="showCreate = true">Create your first project</button></p>
  <article class="card" *ngIf="selected">
    <div class="section-heading"><div><h2>{{ selected.name }} issues</h2><p>Manage the work items for this project.</p></div><div class="card-actions"><a class="button secondary" [routerLink]="['/projects', selected.id, 'board']">Open board</a><button (click)="showIssueForm = !showIssueForm">{{ showIssueForm ? 'Close' : '+ Add issue' }}</button></div></div>
    <div class="create-panel compact" *ngIf="showIssueForm"><div class="form-grid"><label>Title<input [(ngModel)]="newIssue.title" placeholder="What needs to be done?"></label><label>Type<select [(ngModel)]="newIssue.type"><option [ngValue]="1">Task</option><option [ngValue]="2">Bug</option><option [ngValue]="3">Story</option></select></label><label>Priority<select [(ngModel)]="newIssue.priority"><option [ngValue]="1">Low</option><option [ngValue]="2">Medium</option><option [ngValue]="3">High</option><option [ngValue]="4">Critical</option></select></label></div><p *ngIf="issueError" class="error">{{ issueError }}</p><button (click)="createIssue()" [disabled]="creatingIssue">{{ creatingIssue ? 'Adding…' : 'Add issue' }}</button></div>
    <div class="toolbar"><input [(ngModel)]="issueSearch" placeholder="Search issues"><button (click)="loadIssues()">Search</button></div>
    <ul><li *ngFor="let issue of issues"><button class="link" (click)="selectIssue(issue)">#{{ issue.issueNumber }} {{ issue.title }}</button> · status {{ issue.status }} · priority {{ issue.priority }}</li></ul>
    <p *ngIf="!issues.length">No issues found.</p>

    <h3>GitHub repository</h3>
    <div class="toolbar"><input [(ngModel)]="gitHubOwner" placeholder="Owner"><input [(ngModel)]="gitHubName" placeholder="Repository"><button (click)="saveGitHubRepository()">Save</button><button (click)="loadGitHubSummary()" [disabled]="!selected.gitHubRepositoryOwner">Refresh</button></div>
    <p *ngIf="gitHubError" class="error">{{ gitHubError }}</p>
    <div *ngIf="gitHubSummary"><a [href]="gitHubSummary.htmlUrl" target="_blank" rel="noopener">{{ gitHubSummary.owner }}/{{ gitHubSummary.name }}</a><p>{{ gitHubSummary.description }}</p><small>Open issues: {{ gitHubSummary.openIssuesCount }} · Default branch: {{ gitHubSummary.defaultBranch }}</small></div>

    <div *ngIf="selectedIssue">
      <h3>Comments</h3>
      <ul><li *ngFor="let comment of comments">{{ comment.content }}</li></ul>
      <input [(ngModel)]="commentText" placeholder="Add a comment"><button (click)="addComment()">Comment</button>

      <h3>Attachments</h3>
      <ul><li *ngFor="let attachment of attachments"><a [href]="attachment.fileUrl" target="_blank" rel="noopener">{{ attachment.fileName }}</a> ({{ attachment.fileSize }} bytes) <button (click)="deleteAttachment(attachment)">Remove</button></li></ul>
      <input type="file" (change)="onFileSelected($event)">
      <p *ngIf="attachmentError" class="error">{{ attachmentError }}</p>

      <h3>Activity history</h3>
      <ul><li *ngFor="let item of history">{{ item.fieldName }}: {{ item.oldValue || 'none' }} → {{ item.newValue || 'none' }}</li></ul>
    </div>
  </article></section>`
})
export class ProjectsComponent {
  private readonly api = inject(ApiService);
  projects: Project[] = []; issues: Issue[] = []; comments: Comment[] = []; history: History[] = []; attachments: Attachment[] = [];
  organizations: Organization[] = []; selected?: Project; selectedIssue?: Issue; search = ''; issueSearch = ''; commentText = '';
  showCreate = false; showIssueForm = false; creating = false; creatingIssue = false; projectError = ''; issueError = '';
  newProject = { organizationId: '', name: '', key: '', description: '' };
  newIssue = { title: '', type: 1, priority: 2 };
  gitHubOwner = ''; gitHubName = ''; gitHubSummary?: GitHubSummary; gitHubError = ''; attachmentError = '';

  constructor() { this.load(); this.api.get<Organization[]>('/organizations').subscribe(items => this.organizations = items); }
  load() { this.api.get<{ items: Project[] }>(`/projects?search=${encodeURIComponent(this.search)}&page=1&pageSize=50`).subscribe(result => this.projects = result.items); }
  createProject() {
    if (!this.newProject.organizationId || !this.newProject.name.trim() || !this.newProject.key.trim()) { this.projectError = 'Organization, project name, and key are required.'; return; }
    this.creating = true; this.projectError = '';
    this.api.post<string>('/projects', { ...this.newProject, name: this.newProject.name.trim(), key: this.newProject.key.trim().toUpperCase() }).subscribe({
      next: () => { this.newProject = { organizationId: '', name: '', key: '', description: '' }; this.showCreate = false; this.creating = false; this.load(); },
      error: () => { this.projectError = 'Unable to create the project. Check your details and try again.'; this.creating = false; }
    });
  }
  select(project: Project) { this.selected = project; this.selectedIssue = undefined; this.gitHubSummary = undefined; this.gitHubError = ''; this.gitHubOwner = project.gitHubRepositoryOwner ?? ''; this.gitHubName = project.gitHubRepositoryName ?? ''; this.loadIssues(); if (project.gitHubRepositoryOwner) this.loadGitHubSummary(); }
  loadIssues() { if (!this.selected) return; this.api.get<{ items: Issue[] }>(`/projects/${this.selected.id}/issues?search=${encodeURIComponent(this.issueSearch)}&page=1&pageSize=50`).subscribe(result => this.issues = result.items); }
  createIssue() {
    if (!this.selected || !this.newIssue.title.trim()) { this.issueError = 'Issue title is required.'; return; }
    this.creatingIssue = true; this.issueError = '';
    this.api.post<Issue>(`/projects/${this.selected.id}/issues`, { title: this.newIssue.title.trim(), description: null, type: this.newIssue.type, priority: this.newIssue.priority, assigneeId: null }).subscribe({
      next: () => { this.newIssue = { title: '', type: 1, priority: 2 }; this.showIssueForm = false; this.creatingIssue = false; this.loadIssues(); },
      error: () => { this.issueError = 'Unable to add the issue. Try again.'; this.creatingIssue = false; }
    });
  }
  selectIssue(issue: Issue) {
    if (!this.selected) return;
    this.selectedIssue = issue; this.attachmentError = '';
    this.api.get<Comment[]>(`/issues/${issue.id}/comments`).subscribe(result => this.comments = result);
    this.api.get<History[]>(`/issues/${issue.id}/history`).subscribe(result => this.history = result);
    this.loadAttachments();
  }
  addComment() { if (!this.selectedIssue || !this.commentText.trim()) return; this.api.post<Comment>(`/issues/${this.selectedIssue.id}/comments`, { content: this.commentText }).subscribe(() => { this.commentText = ''; this.selectIssue(this.selectedIssue!); }); }

  loadAttachments() { if (!this.selectedIssue) return; this.api.get<Attachment[]>(`/issues/${this.selectedIssue.id}/attachments`).subscribe(result => this.attachments = result); }
  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || !this.selectedIssue) return;
    if (file.size > 25 * 1024 * 1024) { this.attachmentError = 'Attachments must be 25 MB or smaller.'; return; }
    this.attachmentError = '';
    const formData = new FormData();
    formData.append('file', file);
    this.api.post<Attachment>(`/issues/${this.selectedIssue.id}/attachments`, formData).subscribe({
      next: () => this.loadAttachments(),
      error: () => this.attachmentError = 'Failed to upload the attachment. The storage service may be unavailable.'
    });
  }
  deleteAttachment(attachment: Attachment) { this.api.delete<void>(`/attachments/${attachment.id}`).subscribe(() => this.loadAttachments()); }

  saveGitHubRepository() {
    if (!this.selected) return;
    this.api.put<void>(`/projects/${this.selected.id}/github`, { owner: this.gitHubOwner || null, name: this.gitHubName || null }).subscribe(() => {
      this.selected!.gitHubRepositoryOwner = this.gitHubOwner || undefined;
      this.selected!.gitHubRepositoryName = this.gitHubName || undefined;
      this.loadGitHubSummary();
    });
  }
  loadGitHubSummary() {
    if (!this.selected) return;
    this.gitHubError = ''; this.gitHubSummary = undefined;
    this.api.get<GitHubSummary>(`/projects/${this.selected.id}/github/summary`).subscribe({
      next: summary => this.gitHubSummary = summary,
      error: () => this.gitHubError = 'GitHub is unavailable right now. Try again later.'
    });
  }
}
