import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';

interface Organization { id: string; name: string; description?: string; }
interface Team { id: string; name: string; description?: string; }

@Component({ standalone: true, imports: [CommonModule, FormsModule], template: `<section><h1>Organizations</h1><div class="toolbar"><input [(ngModel)]="name" placeholder="New organization name"><button (click)="create()">Create</button></div><div class="card-grid"><article class="card" *ngFor="let org of organizations"><h2>{{ org.name }}</h2><p>{{ org.description || 'No description' }}</p><h3>Teams</h3><ul><li *ngFor="let team of teams[org.id]">{{ team.name }}</li><li *ngIf="!teams[org.id]?.length">No teams yet</li></ul><div class="toolbar"><input [(ngModel)]="teamNames[org.id]" placeholder="New team"><button (click)="createTeam(org.id)">Add team</button></div></article></div></section>` })
export class OrganizationsComponent {
  private readonly api = inject(ApiService); organizations: Organization[] = []; teams: Record<string, Team[]> = {}; teamNames: Record<string, string> = {}; name = '';
  constructor() { this.load(); }
  load() { this.api.get<Organization[]>('/organizations').subscribe(items => { this.organizations = items; items.forEach(org => this.api.get<Team[]>(`/organizations/${org.id}/teams`).subscribe(value => this.teams[org.id] = value)); }); }
  create() { if (!this.name.trim()) return; this.api.post<string>('/organizations', { name: this.name, description: '' }).subscribe(() => { this.name = ''; this.load(); }); }
  createTeam(id: string) { const name = this.teamNames[id]?.trim(); if (!name) return; this.api.post<Team>(`/organizations/${id}/teams`, { name, description: '' }).subscribe(() => { this.teamNames[id] = ''; this.load(); }); }
}
