import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';

interface Project { id: string; organizationId: string; name: string; key: string; status: number; }
interface Issue { id: string; issueNumber: number; title: string; status: number; priority: number; }
interface Comment { id: string; content: string; authorId: string; createdAtUtc: string; }
interface History { id: string; fieldName: string; oldValue?: string; newValue?: string; createdAtUtc: string; }

@Component({ standalone: true, imports: [CommonModule, FormsModule], template: `<section><h1>Projects</h1><div class="toolbar"><input [(ngModel)]="search" placeholder="Search projects"><button (click)="load()">Search</button></div><div class="card-grid"><article class="card" *ngFor="let project of projects"><h2>{{ project.key }} · {{ project.name }}</h2><p>Status: {{ project.status }}</p><button (click)="select(project)">View issues</button></article></div><article class="card" *ngIf="selected"><h2>{{ selected.name }} issues</h2><div class="toolbar"><input [(ngModel)]="issueSearch" placeholder="Search issues"><button (click)="loadIssues()">Search</button></div><ul><li *ngFor="let issue of issues"><button class="link" (click)="selectIssue(issue)">#{{ issue.issueNumber }} {{ issue.title }}</button> · status {{ issue.status }} · priority {{ issue.priority }}</li></ul><p *ngIf="!issues.length">No issues found.</p><div *ngIf="selectedIssue"><h3>Comments</h3><ul><li *ngFor="let comment of comments">{{ comment.content }}</li></ul><input [(ngModel)]="commentText" placeholder="Add a comment"><button (click)="addComment()">Comment</button><h3>Activity history</h3><ul><li *ngFor="let item of history">{{ item.fieldName }}: {{ item.oldValue || 'none' }} → {{ item.newValue || 'none' }}</li></ul></div></article></section>` })
export class ProjectsComponent {
  private readonly api = inject(ApiService); projects: Project[] = []; issues: Issue[] = []; comments: Comment[] = []; history: History[] = []; selected?: Project; selectedIssue?: Issue; search = ''; issueSearch = ''; commentText = '';
  constructor() { this.load(); }
  load() { this.api.get<{ items: Project[] }>('/projects?page=1&pageSize=50').subscribe(result => this.projects = result.items); }
  select(project: Project) { this.selected = project; this.selectedIssue = undefined; this.loadIssues(); }
  loadIssues() { if (!this.selected) return; this.api.get<{ items: Issue[] }>(`/projects/${this.selected.id}/issues?search=${encodeURIComponent(this.issueSearch)}&page=1&pageSize=50`).subscribe(result => this.issues = result.items); }
  selectIssue(issue: Issue) { if (!this.selected) return; this.selectedIssue = issue; this.api.get<Comment[]>(`/issues/${issue.id}/comments`).subscribe(result => this.comments = result); this.api.get<History[]>(`/issues/${issue.id}/history`).subscribe(result => this.history = result); }
  addComment() { if (!this.selectedIssue || !this.commentText.trim()) return; this.api.post<Comment>(`/issues/${this.selectedIssue.id}/comments`, { content: this.commentText }).subscribe(() => { this.commentText = ''; this.selectIssue(this.selectedIssue!); }); }
}
