import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ISSUE_PRIORITIES, ISSUE_STATUSES, ISSUE_TYPES, Issue, IssuePriority, IssueStatus, Page, Sprint, SprintStatus } from '../../core/models';
import { errorMessage } from '../../core/problem';
import { RealtimeService } from '../../core/realtime.service';
import { ToastService } from '../../core/toast.service';
import { AvatarComponent } from '../../shared/avatar.component';
import { LabelPipe } from '../../shared/pipes';
import { IssueFormDialogComponent } from './issue-form-dialog.component';
import { ProjectContext } from './project-context';

@Component({
  selector: 'app-board',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, AvatarComponent, LabelPipe, IssueFormDialogComponent],
  template: `
    <div class="toolbar">
      <input class="input search" type="search" placeholder="Filter cards…" aria-label="Filter cards" [ngModel]="filterText()" (ngModelChange)="filterText.set($event)" />
      <select class="select" aria-label="Priority" [ngModel]="priority()" (ngModelChange)="priority.set($event)">
        <option [ngValue]="null">All priorities</option>
        @for (option of priorities; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
      </select>
      @if (activeSprint(); as sprint) {
        <label class="row" style="font-size: .9rem; cursor: pointer">
          <input type="checkbox" [ngModel]="sprintOnly()" (ngModelChange)="sprintOnly.set($event)" /> Only {{ sprint.name }}
        </label>
      }
      <span class="spacer"></span>
      @if (!canMove()) { <span class="subtle">Read-only: viewers can't move cards</span> }
      @if (canMove()) { <button class="btn" type="button" (click)="showCreate.set(true)">+ New issue</button> }
    </div>

    @if (loading()) {
      <div class="board">@for (column of columns; track column.value) { <div class="column"><div class="skeleton block"></div></div> }</div>
    } @else {
      <div class="board board-wide" role="list" aria-label="Board columns" (dragover)="autoScroll($event)">
        @for (column of columns; track column.value) {
          <section class="column" role="listitem" [class.drop-target]="dropTarget() === column.value" [attr.aria-label]="column.label"
                   (dragover)="onDragOver($event, column.value)" (dragleave)="dropTarget.set(null)" (drop)="onDrop($event, column.value)">
            <header class="column-header">
              <span>{{ column.label }}</span>
              <span class="badge">{{ byStatus().get(column.value)?.length ?? 0 }}</span>
            </header>
            @for (issue of byStatus().get(column.value) ?? []; track issue.id) {
              <!-- Not a <button>: Firefox will not start an HTML5 drag from a button element. -->
              <article class="issue-card" role="button" tabindex="0" [attr.draggable]="canMove()" [class.dragging]="dragging()?.id === issue.id"
                       (dragstart)="onDragStart($event, issue)" (dragend)="dragging.set(null); dropTarget.set(null)"
                       (click)="open(issue)" (keydown.enter)="open(issue)" (keydown.space)="$event.preventDefault(); open(issue)"
                       [attr.aria-label]="key() + '-' + issue.issueNumber + ': ' + issue.title">
                <span class="row between">
                  <span class="num subtle mono">{{ key() }}-{{ issue.issueNumber }}</span>
                  <span class="badge" [class]="'badge type-' + issue.type">{{ issue.type | label: types }}</span>
                </span>
                <span class="title">{{ issue.title }}</span>
                <span class="meta">
                  <span class="badge" [class]="'badge priority-' + issue.priority">{{ issue.priority | label: priorities }}</span>
                  <app-avatar [name]="context.nameOf(issue.assigneeId)" [seed]="issue.assigneeId" size="sm" />
                </span>
              </article>
            } @empty {
              <p class="subtle" style="text-align: center; padding: 16px 0">No issues</p>
            }
          </section>
        }
      </div>
    }

    @if (showCreate()) {
      <app-issue-form-dialog [projectId]="projectId()" [members]="context.members()" (created)="onCreated($event)" (closed)="showCreate.set(false)" />
    }
  `,
})
export class BoardComponent {
  readonly context = inject(ProjectContext);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  readonly columns = ISSUE_STATUSES;
  readonly priorities = ISSUE_PRIORITIES;
  readonly types = ISSUE_TYPES;

  readonly projectId = computed(() => this.context.project()?.id ?? '');
  readonly key = computed(() => this.context.project()?.key ?? '');
  readonly canMove = computed(() => this.context.permissions().canContribute);
  readonly issues = signal<Issue[]>([]);
  readonly sprints = signal<Sprint[]>([]);
  readonly loading = signal(true);
  readonly filterText = signal('');
  readonly priority = signal<IssuePriority | null>(null);
  readonly sprintOnly = signal(false);
  readonly dragging = signal<Issue | null>(null);
  readonly dropTarget = signal<IssueStatus | null>(null);
  readonly showCreate = signal(false);

  readonly activeSprint = computed(() => this.sprints().find(sprint => sprint.status === SprintStatus.Active) ?? null);
  readonly byStatus = computed(() => {
    const text = this.filterText().trim().toLowerCase();
    const priority = this.priority();
    const sprintId = this.sprintOnly() ? this.activeSprint()?.id : undefined;
    const groups = new Map<IssueStatus, Issue[]>();

    for (const issue of this.issues()) {
      if (text && !issue.title.toLowerCase().includes(text) && !`${this.key()}-${issue.issueNumber}`.toLowerCase().includes(text)) continue;
      if (priority !== null && issue.priority !== priority) continue;
      if (sprintId && issue.sprintId !== sprintId) continue;
      const list = groups.get(issue.status) ?? [];
      list.push(issue);
      groups.set(issue.status, list);
    }

    for (const list of groups.values()) list.sort((a, b) => b.priority - a.priority || a.issueNumber - b.issueNumber);
    return groups;
  });

  constructor() {
    const realtime = inject(RealtimeService);
    const destroyRef = inject(DestroyRef);
    realtime.issueCreated$.pipe(takeUntilDestroyed(destroyRef)).subscribe(issue => this.upsert(issue));
    realtime.issueUpdated$.pipe(takeUntilDestroyed(destroyRef)).subscribe(issue => this.upsert(issue));
    realtime.issueDeleted$.pipe(takeUntilDestroyed(destroyRef)).subscribe(id => this.issues.update(items => items.filter(item => item.id !== id)));

    this.load();
  }

  load(): void {
    const projectId = this.projectId();
    this.api.get<Page<Issue>>(`/projects/${projectId}/issues`, { pageSize: 100, sortBy: 'priority', descending: true }).subscribe({
      next: page => {
        this.issues.set(page.items);
        this.loading.set(false);
        if (page.totalCount > page.items.length) {
          this.toasts.info(`Showing the ${page.items.length} highest-priority of ${page.totalCount} issues.`);
        }
      },
      error: () => this.loading.set(false),
    });
    this.api.get<Sprint[]>(`/projects/${projectId}/sprints`).subscribe(items => {
      this.sprints.set(items);
      if (items.some(sprint => sprint.status === SprintStatus.Active)) this.sprintOnly.set(true);
    });
  }

  onDragStart(event: DragEvent, issue: Issue): void {
    if (!this.canMove()) {
      event.preventDefault();
      return;
    }
    this.dragging.set(issue);
    event.dataTransfer?.setData('text/plain', issue.id);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
  }

  onDragOver(event: DragEvent, status: IssueStatus): void {
    if (!this.dragging()) return;
    event.preventDefault();
    this.dropTarget.set(status);
  }

  /** Native drag-and-drop doesn't scroll overflow containers, so nudge the board when dragging near its edges. */
  autoScroll(event: DragEvent): void {
    if (!this.dragging()) return;
    const board = event.currentTarget as HTMLElement;
    const rect = board.getBoundingClientRect();
    const edge = 80;
    if (event.clientX > rect.right - edge) board.scrollLeft += 24;
    else if (event.clientX < rect.left + edge) board.scrollLeft -= 24;
  }

  onDrop(event: DragEvent, status: IssueStatus): void {
    event.preventDefault();
    const issue = this.dragging();
    this.dragging.set(null);
    this.dropTarget.set(null);
    if (issue && issue.status !== status) this.move(issue, status);
  }

  /** Optimistically moves the card, rolling back if the API rejects the change. */
  move(issue: Issue, status: IssueStatus): void {
    this.upsert({ ...issue, status });
    const body = { title: issue.title, description: issue.description ?? null, type: issue.type, status, priority: issue.priority, assigneeId: issue.assigneeId ?? null };
    this.api.put<void>(`/issues/${issue.id}`, body).subscribe({
      error: error => {
        this.upsert(issue);
        this.toasts.error(errorMessage(error, 'Could not move the issue.'));
      },
    });
  }

  open(issue: Issue): void {
    void this.router.navigate(['/projects', issue.projectId, 'issues', issue.id]);
  }

  onCreated(issue: Issue): void {
    this.showCreate.set(false);
    this.upsert(issue);
    this.toasts.success(`${this.key()}-${issue.issueNumber} created.`);
  }

  private upsert(issue: Issue): void {
    if (issue.projectId !== this.projectId()) return;
    this.issues.update(items => {
      const index = items.findIndex(item => item.id === issue.id);
      if (index < 0) return [...items, issue];
      const copy = [...items];
      copy[index] = issue;
      return copy;
    });
  }
}
