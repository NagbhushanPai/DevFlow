import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ConfirmService } from '../../core/confirm.service';
import {
  ATTACHMENT_EXTENSIONS, Attachment, Comment, HistoryEntry, ISSUE_PRIORITIES, ISSUE_STATUSES, ISSUE_TYPES, Issue,
  MAX_ATTACHMENT_BYTES, Option, Sprint,
} from '../../core/models';
import { errorMessage } from '../../core/problem';
import { RealtimeService } from '../../core/realtime.service';
import { ToastService } from '../../core/toast.service';
import { AvatarComponent } from '../../shared/avatar.component';
import { FileSizePipe, LabelPipe, RelativeTimePipe, ShortDatePipe } from '../../shared/pipes';
import { ProjectContext } from './project-context';

type EditableField = 'status' | 'priority' | 'type' | 'assigneeId';

@Component({
  selector: 'app-issue-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, AvatarComponent, LabelPipe, RelativeTimePipe, FileSizePipe, ShortDatePipe],
  template: `
    @if (issue(); as issue) {
      <nav class="breadcrumbs"><a routerLink="../">Issues</a> / <span class="mono">{{ key() }}-{{ issue.issueNumber }}</span></nav>

      <div class="split">
        <div class="stack">
          <div class="card">
            @if (editing()) {
              <form (ngSubmit)="saveText()" #form="ngForm">
                <div class="field">
                  <label for="title">Title</label>
                  <input id="title" class="input" name="title" required maxlength="300" [(ngModel)]="draftTitle" />
                </div>
                <div class="field">
                  <label for="description">Description</label>
                  <textarea id="description" class="textarea" name="description" rows="8" maxlength="5000" [(ngModel)]="draftDescription"></textarea>
                </div>
                <div class="form-actions">
                  <button class="btn secondary" type="button" (click)="editing.set(false)">Cancel</button>
                  <button class="btn" type="submit" [disabled]="saving() || form.invalid">Save</button>
                </div>
              </form>
            } @else {
              <div class="row between" style="align-items: flex-start">
                <h1 style="flex: 1">{{ issue.title }}</h1>
                @if (canEdit()) {
                  <button class="btn secondary sm" type="button" (click)="startEditing(issue)">Edit</button>
                }
              </div>
              @if (issue.description) {
                <p class="description">{{ issue.description }}</p>
              } @else {
                <p class="subtle">No description.</p>
              }
            }
          </div>

          <div class="card">
            <div class="card-header"><h2>Attachments</h2><span class="subtle">{{ attachments().length }}</span></div>
            <ul class="list">
              @for (attachment of attachments(); track attachment.id) {
                <li>
                  <span aria-hidden="true">📎</span>
                  <button class="link-btn person-name" type="button" (click)="download(attachment)">{{ attachment.fileName }}</button>
                  <span class="subtle">{{ attachment.fileSize | fileSize }}</span>
                  <span class="spacer"></span>
                  <span class="subtle hide-mobile">{{ context.nameOf(attachment.uploadedById) }} · {{ attachment.createdAtUtc | relativeTime }}</span>
                  @if (context.permissions().canDeleteOwned(attachment.uploadedById, context.userId())) {
                    <button class="btn danger-ghost sm" type="button" (click)="deleteAttachment(attachment)" [attr.aria-label]="'Delete ' + attachment.fileName">Delete</button>
                  }
                </li>
              }
            </ul>
            @if (canEdit()) {
              <label class="dropzone" [class.active]="dragOver()" style="display: block; margin-top: 12px; cursor: pointer"
                     (dragover)="$event.preventDefault(); dragOver.set(true)" (dragleave)="dragOver.set(false)" (drop)="onDrop($event)">
                @if (uploading()) {
                  <span class="spinner"></span> Uploading…
                } @else {
                  Drop a file here or <span class="link-btn">browse</span> · max 25 MB
                }
                <input type="file" class="sr-only" [accept]="accept" (change)="onFileInput($event)" />
              </label>
            } @else if (!attachments().length) {
              <p class="subtle">No attachments.</p>
            }
          </div>

          <div class="card">
            <div class="card-header"><h2>Comments</h2><span class="subtle">{{ comments().length }}</span></div>
            <ul class="timeline">
              @for (comment of comments(); track comment.id) {
                <li>
                  <app-avatar [name]="context.nameOf(comment.authorId)" [seed]="comment.authorId" />
                  <div style="flex: 1; min-width: 0">
                    <div class="row">
                      <strong>{{ context.nameOf(comment.authorId) }}</strong>
                      <span class="subtle">{{ comment.createdAtUtc | relativeTime }}</span>
                      <span class="spacer"></span>
                      @if (context.permissions().canDeleteOwned(comment.authorId, context.userId()) || comment.authorId === context.userId()) {
                        <button class="btn ghost sm" type="button" (click)="deleteComment(comment)">Delete</button>
                      }
                    </div>
                    <p class="comment-body">{{ comment.content }}</p>
                  </div>
                </li>
              } @empty {
                <li class="subtle">No comments yet.</li>
              }
            </ul>
            @if (canEdit()) {
              <form (ngSubmit)="addComment()" style="margin-top: 12px">
                <textarea class="textarea" name="comment" rows="3" maxlength="5000" placeholder="Write a comment…" aria-label="New comment"
                          [(ngModel)]="commentText" (keydown.control.enter)="addComment()" (keydown.meta.enter)="addComment()"></textarea>
                <div class="form-actions">
                  <span class="subtle" style="margin-right: auto; align-self: center">Ctrl + Enter to send</span>
                  <button class="btn" type="submit" [disabled]="!commentText.trim() || posting()">Comment</button>
                </div>
              </form>
            }
          </div>

          <div class="card">
            <div class="card-header"><h2>Activity</h2></div>
            <ul class="timeline">
              @for (entry of history(); track entry.id) {
                <li>
                  <app-avatar [name]="context.nameOf(entry.changedById)" [seed]="entry.changedById" size="sm" />
                  <span>
                    <strong>{{ context.nameOf(entry.changedById) }}</strong>
                    changed <strong>{{ fieldLabel(entry.fieldName) }}</strong>
                    from <em>{{ formatValue(entry.fieldName, entry.oldValue) }}</em>
                    to <em>{{ formatValue(entry.fieldName, entry.newValue) }}</em>
                    <span class="subtle"> · {{ entry.createdAtUtc | relativeTime }}</span>
                  </span>
                </li>
              } @empty {
                <li class="subtle">No changes yet.</li>
              }
            </ul>
          </div>
        </div>

        <aside class="card">
          <dl class="meta-list">
            <dt>Status</dt>
            <dd>
              @if (canEdit()) {
                <select class="select" aria-label="Status" [ngModel]="issue.status" (ngModelChange)="update('status', $event)">
                  @for (option of statuses; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
                </select>
              } @else { <span class="badge" [class]="'badge status-' + issue.status">{{ issue.status | label: statuses }}</span> }
            </dd>
            <dt>Priority</dt>
            <dd>
              @if (canEdit()) {
                <select class="select" aria-label="Priority" [ngModel]="issue.priority" (ngModelChange)="update('priority', $event)">
                  @for (option of priorities; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
                </select>
              } @else { <span class="badge" [class]="'badge priority-' + issue.priority">{{ issue.priority | label: priorities }}</span> }
            </dd>
            <dt>Type</dt>
            <dd>
              @if (canEdit()) {
                <select class="select" aria-label="Type" [ngModel]="issue.type" (ngModelChange)="update('type', $event)">
                  @for (option of types; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
                </select>
              } @else { <span class="badge" [class]="'badge type-' + issue.type">{{ issue.type | label: types }}</span> }
            </dd>
            <dt>Assignee</dt>
            <dd>
              @if (canEdit()) {
                <select class="select" aria-label="Assignee" [ngModel]="issue.assigneeId ?? null" (ngModelChange)="update('assigneeId', $event)">
                  <option [ngValue]="null">Unassigned</option>
                  @for (member of context.members(); track member.userId) { <option [ngValue]="member.userId">{{ member.fullName }}</option> }
                </select>
              } @else { <app-avatar [name]="context.nameOf(issue.assigneeId)" [seed]="issue.assigneeId" size="sm" [showName]="true" /> }
            </dd>
            <dt>Reporter</dt>
            <dd><app-avatar [name]="context.nameOf(issue.reporterId)" [seed]="issue.reporterId" size="sm" [showName]="true" /></dd>
            <dt>Sprint</dt>
            <dd>{{ sprintName() }}</dd>
            <dt>Created</dt>
            <dd>{{ issue.createdAtUtc | shortDate }}</dd>
            <dt>Updated</dt>
            <dd>{{ issue.updatedAtUtc ?? issue.createdAtUtc | relativeTime }}</dd>
          </dl>
          @if (context.permissions().canDeleteIssue(issue.reporterId, context.userId())) {
            <hr style="border: 0; border-top: 1px solid var(--border); margin: 20px 0 16px" />
            <button class="btn danger-ghost block" type="button" (click)="deleteIssue(issue)">Delete issue</button>
          }
        </aside>
      </div>
    } @else if (notFound()) {
      <div class="card empty">
        <h3>Issue not found</h3>
        <p>It may have been deleted.</p>
        <a class="btn" routerLink="../">Back to issues</a>
      </div>
    } @else {
      <div class="split"><div class="skeleton block" style="height: 240px"></div><div class="skeleton block" style="height: 240px"></div></div>
    }
  `,
})
export class IssueDetailComponent {
  readonly context = inject(ProjectContext);
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  readonly issueId = input.required<string>();

  readonly statuses = ISSUE_STATUSES;
  readonly priorities = ISSUE_PRIORITIES;
  readonly types = ISSUE_TYPES;
  readonly accept = ATTACHMENT_EXTENSIONS.join(',');

  readonly issue = signal<Issue | null>(null);
  readonly notFound = signal(false);
  readonly comments = signal<Comment[]>([]);
  readonly attachments = signal<Attachment[]>([]);
  readonly history = signal<HistoryEntry[]>([]);
  readonly sprints = signal<Sprint[]>([]);
  readonly editing = signal(false);
  readonly saving = signal(false);
  readonly posting = signal(false);
  readonly uploading = signal(false);
  readonly dragOver = signal(false);

  readonly key = computed(() => this.context.project()?.key ?? '');
  readonly canEdit = computed(() => this.context.permissions().canContribute);
  readonly sprintName = computed(() => {
    const id = this.issue()?.sprintId;
    return id ? (this.sprints().find(sprint => sprint.id === id)?.name ?? 'Sprint') : 'Backlog';
  });

  draftTitle = '';
  draftDescription = '';
  commentText = '';

  constructor() {
    effect(() => this.load(this.issueId()));

    const realtime = inject(RealtimeService);
    const destroyRef = inject(DestroyRef);
    const isThisIssue = (id: string) => id === this.issueId();

    realtime.issueUpdated$.pipe(filter(issue => isThisIssue(issue.id)), takeUntilDestroyed(destroyRef)).subscribe(issue => {
      this.issue.set(issue);
      this.loadHistory();
    });
    realtime.issueDeleted$.pipe(filter(isThisIssue), takeUntilDestroyed(destroyRef)).subscribe(() => {
      this.toasts.info('This issue was deleted.');
      void this.router.navigate(['/projects', this.context.project()?.id, 'issues']);
    });
    realtime.commentAdded$.pipe(filter(comment => isThisIssue(comment.issueId)), takeUntilDestroyed(destroyRef)).subscribe(comment =>
      this.comments.update(items => (items.some(item => item.id === comment.id) ? items : [...items, comment])));
    realtime.commentDeleted$.pipe(filter(event => isThisIssue(event.issueId)), takeUntilDestroyed(destroyRef)).subscribe(event =>
      this.comments.update(items => items.filter(item => item.id !== event.commentId)));
  }

  load(issueId: string): void {
    this.issue.set(null);
    this.notFound.set(false);
    this.api.get<Issue>(`/issues/${issueId}`).subscribe({
      next: issue => this.issue.set(issue),
      error: () => this.notFound.set(true),
    });
    this.api.get<Comment[]>(`/issues/${issueId}/comments`).subscribe(items => this.comments.set(items));
    this.api.get<Attachment[]>(`/issues/${issueId}/attachments`).subscribe(items => this.attachments.set(items));
    this.loadHistory();
    const projectId = this.context.project()?.id;
    if (projectId) this.api.get<Sprint[]>(`/projects/${projectId}/sprints`).subscribe(items => this.sprints.set(items));
  }

  startEditing(issue: Issue): void {
    this.draftTitle = issue.title;
    this.draftDescription = issue.description ?? '';
    this.editing.set(true);
  }

  saveText(): void {
    const issue = this.issue();
    if (!issue) return;
    this.saving.set(true);
    this.put({ ...issue, title: this.draftTitle.trim(), description: this.draftDescription.trim() || null }, () => {
      this.editing.set(false);
      this.saving.set(false);
    }, () => this.saving.set(false));
  }

  update(field: EditableField, value: unknown): void {
    const issue = this.issue();
    if (!issue || issue[field] === value) return;
    const previous = issue;
    const updated = { ...issue, [field]: value } as Issue;
    this.issue.set(updated); // optimistic
    this.put(updated, undefined, () => this.issue.set(previous));
  }

  addComment(): void {
    const content = this.commentText.trim();
    if (!content || this.posting()) return;
    this.posting.set(true);
    this.api.post<Comment>(`/issues/${this.issueId()}/comments`, { content }).subscribe({
      next: comment => {
        this.comments.update(items => (items.some(item => item.id === comment.id) ? items : [...items, comment]));
        this.commentText = '';
        this.posting.set(false);
      },
      error: error => {
        this.toasts.error(errorMessage(error, 'Could not post the comment.'));
        this.posting.set(false);
      },
    });
  }

  async deleteComment(comment: Comment): Promise<void> {
    if (!(await this.confirm.ask({ title: 'Delete comment?', message: 'This cannot be undone.', confirmLabel: 'Delete', danger: true }))) return;
    this.api.delete<void>(`/comments/${comment.id}`).subscribe({
      next: () => this.comments.update(items => items.filter(item => item.id !== comment.id)),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) this.upload(file);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.upload(file);
  }

  upload(file: File): void {
    const extension = file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : '';
    if (!ATTACHMENT_EXTENSIONS.includes(extension)) {
      this.toasts.error(`Files of type "${extension || 'unknown'}" are not allowed.`);
      return;
    }
    if (file.size > MAX_ATTACHMENT_BYTES) {
      this.toasts.error('Attachments must be 25 MB or smaller.');
      return;
    }

    const form = new FormData();
    form.append('file', file);
    this.uploading.set(true);
    this.api.post<Attachment>(`/issues/${this.issueId()}/attachments`, form).subscribe({
      next: attachment => {
        this.attachments.update(items => [...items, attachment]);
        this.uploading.set(false);
        this.toasts.success(`${attachment.fileName} uploaded.`);
      },
      error: error => {
        this.uploading.set(false);
        this.toasts.error(errorMessage(error, 'Upload failed.'));
      },
    });
  }

  download(attachment: Attachment): void {
    this.api.download(attachment.downloadUrl, attachment.fileName).subscribe({ error: error => this.toasts.error(errorMessage(error, 'Download failed.')) });
  }

  async deleteAttachment(attachment: Attachment): Promise<void> {
    if (!(await this.confirm.ask({ title: `Delete ${attachment.fileName}?`, message: 'The file will be removed permanently.', confirmLabel: 'Delete', danger: true }))) return;
    this.api.delete<void>(`/attachments/${attachment.id}`).subscribe({
      next: () => this.attachments.update(items => items.filter(item => item.id !== attachment.id)),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  async deleteIssue(issue: Issue): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: `Delete ${this.key()}-${issue.issueNumber}?`,
      message: 'The issue and its comments will be removed from the project.',
      confirmLabel: 'Delete issue',
      danger: true,
    });
    if (!confirmed) return;

    this.api.delete<void>(`/issues/${issue.id}`).subscribe({
      next: () => {
        this.toasts.success('Issue deleted.');
        void this.router.navigate(['/projects', issue.projectId, 'issues']);
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  fieldLabel(field: string): string {
    return field === 'AssigneeId' ? 'assignee' : field.toLowerCase();
  }

  /** History stores enum names ("InProgress") and user ids; show them as people-friendly text. */
  formatValue(field: string, value: string | null | undefined): string {
    if (value === null || value === undefined || value === '') return 'none';
    if (field === 'AssigneeId') return this.context.nameOf(value);
    const options: Record<string, Option<number>[]> = { Status: this.statuses, Priority: this.priorities, Type: this.types };
    const list = options[field];
    if (list) {
      const match = list.find(option => option.label.replace(/\s/g, '').toLowerCase() === value.toLowerCase());
      if (match) return match.label;
    }
    return value.length > 80 ? `${value.slice(0, 80)}…` : value;
  }

  private put(issue: Issue, done?: () => void, failed?: () => void): void {
    const body = {
      title: issue.title,
      description: issue.description ?? null,
      type: issue.type,
      status: issue.status,
      priority: issue.priority,
      assigneeId: issue.assigneeId ?? null,
    };
    this.api.put<void>(`/issues/${issue.id}`, body).subscribe({
      next: () => {
        this.issue.set({ ...issue, updatedAtUtc: new Date().toISOString() });
        this.loadHistory();
        done?.();
      },
      error: error => {
        this.toasts.error(errorMessage(error, 'Could not save the change.'));
        failed?.();
      },
    });
  }

  private loadHistory(): void {
    this.api.get<HistoryEntry[]>(`/issues/${this.issueId()}/history`).subscribe(items => this.history.set(items));
  }
}
