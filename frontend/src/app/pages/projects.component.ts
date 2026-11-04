import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';

interface Project { id: string; organizationId: string; name: string; key: string; status: number; gitHubRepositoryOwner?: string; gitHubRepositoryName?: string; }
interface Issue { id: string; issueNumber: number; title: string; status: number; priority: number; }
interface Comment { id: string; content: string; authorId: string; createdAtUtc: string; }
interface History { id: string; fieldName: string; oldValue?: string; newValue?: string; createdAtUtc: string; }
interface Attachment { id: string; fileName: string; fileUrl: string; contentType: string; fileSize: number; createdAtUtc: string; }
interface GitHubSummary { owner: string; name: string; description?: string; openIssuesCount: number; defaultBranch: string; htmlUrl: string; }

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `<section><h1>Projects</h1><div class="toolbar"><input [(ngModel)]="search" placeholder="Search projects"><button (click)="load()">Search</button></div><div class="card-grid"><article class="card" *ngFor="let project of projects"><h2>{{ project.key }} · {{ project.name }}</h2><p>Status: {{ project.status }}</p><button (click)="select(project)">View issues</button></article></div>
  <article class="card" *ngIf="selected">
    <h2>{{ selected.name }} issues</h2>
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
  selected?: Project; selectedIssue?: Issue; search = ''; issueSearch = ''; commentText = '';
  gitHubOwner = ''; gitHubName = ''; gitHubSummary?: GitHubSummary; gitHubError = ''; attachmentError = '';

  constructor() { this.load(); }
  load() { this.api.get<{ items: Project[] }>('/projects?page=1&pageSize=50').subscribe(result => this.projects = result.items); }
  select(project: Project) { this.selected = project; this.selectedIssue = undefined; this.gitHubSummary = undefined; this.gitHubError = ''; this.gitHubOwner = project.gitHubRepositoryOwner ?? ''; this.gitHubName = project.gitHubRepositoryName ?? ''; this.loadIssues(); if (project.gitHubRepositoryOwner) this.loadGitHubSummary(); }
  loadIssues() { if (!this.selected) return; this.api.get<{ items: Issue[] }>(`/projects/${this.selected.id}/issues?search=${encodeURIComponent(this.issueSearch)}&page=1&pageSize=50`).subscribe(result => this.issues = result.items); }
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
