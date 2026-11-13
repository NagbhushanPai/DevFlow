import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, merge } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ISSUE_PRIORITIES, ISSUE_STATUSES, ISSUE_TYPES, Issue, IssuePriority, IssueStatus, Page } from '../../core/models';
import { RealtimeService } from '../../core/realtime.service';
import { ToastService } from '../../core/toast.service';
import { AvatarComponent } from '../../shared/avatar.component';
import { LabelPipe, RelativeTimePipe } from '../../shared/pipes';
import { IssueFormDialogComponent } from './issue-form-dialog.component';
import { ProjectContext } from './project-context';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-issue-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, AvatarComponent, LabelPipe, RelativeTimePipe, IssueFormDialogComponent],
  template: `
    <div class="toolbar">
      <input class="input search" type="search" placeholder="Search issues…" aria-label="Search issues"
             [ngModel]="search()" (ngModelChange)="search.set($event); changes.next()" />
      <select class="select" aria-label="Status" [ngModel]="status()" (ngModelChange)="status.set($event); reset()">
        <option [ngValue]="null">All statuses</option>
        @for (option of statuses; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
      </select>
      <select class="select" aria-label="Priority" [ngModel]="priority()" (ngModelChange)="priority.set($event); reset()">
        <option [ngValue]="null">All priorities</option>
        @for (option of priorities; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
      </select>
      <select class="select" aria-label="Assignee" [ngModel]="assigneeId()" (ngModelChange)="assigneeId.set($event); reset()">
        <option [ngValue]="null">Anyone</option>
        <option [ngValue]="context.userId()">Assigned to me</option>
        @for (member of context.members(); track member.userId) {
          @if (member.userId !== context.userId()) { <option [ngValue]="member.userId">{{ member.fullName }}</option> }
        }
      </select>
      <select class="select" aria-label="Sort" [ngModel]="sort()" (ngModelChange)="sort.set($event); reset()">
        <option value="number-desc">Newest</option>
        <option value="number-asc">Oldest</option>
        <option value="priority-desc">Priority</option>
        <option value="status-asc">Status</option>
      </select>
      <span class="spacer"></span>
      @if (context.permissions().canContribute) {
        <button class="btn" type="button" (click)="showCreate.set(true)">+ New issue</button>
      }
    </div>

    <div class="card flush">
      @if (loading() && !page()) {
        <div style="padding: 16px" class="stack"><div class="skeleton"></div><div class="skeleton"></div><div class="skeleton"></div></div>
      } @else if (page()?.items?.length) {
        <div class="table-wrap">
          <table class="table">
            <thead>
              <tr><th>ID</th><th>Title</th><th>Status</th><th>Priority</th><th class="hide-mobile">Assignee</th><th class="hide-mobile">Updated</th></tr>
            </thead>
            <tbody>
              @for (issue of page()!.items; track issue.id) {
                <tr class="clickable" (click)="open(issue)" (keydown.enter)="open(issue)" tabindex="0">
                  <td class="num">{{ key() }}-{{ issue.issueNumber }}</td>
                  <td>
                    <span class="badge" [class]="'badge type-' + issue.type" style="margin-right: 6px">{{ issue.type | label: types }}</span>
                    <a [routerLink]="[issue.id]" (click)="$event.stopPropagation()">{{ issue.title }}</a>
                  </td>
                  <td><span class="badge" [class]="'badge status-' + issue.status">{{ issue.status | label: statuses }}</span></td>
                  <td><span class="badge" [class]="'badge priority-' + issue.priority">{{ issue.priority | label: priorities }}</span></td>
                  <td class="hide-mobile">
                    <app-avatar [name]="context.nameOf(issue.assigneeId)" [seed]="issue.assigneeId" size="sm" [showName]="true" />
                  </td>
                  <td class="hide-mobile subtle">{{ issue.updatedAtUtc ?? issue.createdAtUtc | relativeTime }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pagination">
          <span>{{ rangeLabel() }}</span>
          <div class="row">
            <button class="btn secondary sm" type="button" [disabled]="pageNumber() <= 1" (click)="go(pageNumber() - 1)">Previous</button>
            <button class="btn secondary sm" type="button" [disabled]="pageNumber() >= totalPages()" (click)="go(pageNumber() + 1)">Next</button>
          </div>
        </div>
      } @else {
        <div class="empty">
          @if (hasFilters()) {
            <h3>No issues match your filters</h3>
            <button class="btn secondary" type="button" (click)="clearFilters()">Clear filters</button>
          } @else {
            <h3>No issues yet</h3>
            <p>Issues track tasks, bugs and stories for this project.</p>
            @if (context.permissions().canContribute) {
              <button class="btn" type="button" (click)="showCreate.set(true)">Create the first issue</button>
            }
          }
        </div>
      }
    </div>

    @if (showCreate()) {
      <app-issue-form-dialog [projectId]="projectId()" [members]="context.members()" (created)="onCreated($event)" (closed)="showCreate.set(false)" />
    }
  `,
})
export class IssueListComponent {
  readonly context = inject(ProjectContext);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  readonly statuses = ISSUE_STATUSES;
  readonly priorities = ISSUE_PRIORITIES;
  readonly types = ISSUE_TYPES;

  readonly projectId = computed(() => this.context.project()?.id ?? '');
  readonly key = computed(() => this.context.project()?.key ?? '');
  readonly page = signal<Page<Issue> | null>(null);
  readonly loading = signal(true);
  readonly pageNumber = signal(1);
  readonly search = signal('');
  readonly status = signal<IssueStatus | null>(null);
  readonly priority = signal<IssuePriority | null>(null);
  readonly assigneeId = signal<string | null>(null);
  readonly sort = signal('number-desc');
  readonly showCreate = signal(false);
  readonly changes = new Subject<void>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil((this.page()?.totalCount ?? 0) / PAGE_SIZE)));
  readonly hasFilters = computed(() => !!this.search().trim() || this.status() !== null || this.priority() !== null || this.assigneeId() !== null);
  readonly rangeLabel = computed(() => {
    const page = this.page();
    if (!page || page.totalCount === 0) return '';
    const start = (page.page - 1) * page.pageSize + 1;
    return `${start}–${start + page.items.length - 1} of ${page.totalCount}`;
  });

  constructor() {
    const realtime = inject(RealtimeService);
    const destroyRef = inject(DestroyRef);

    this.changes.pipe(debounceTime(250), takeUntilDestroyed(destroyRef)).subscribe(() => this.reset());
    // Any change by anyone in this project refreshes the current page (debounced to batch bursts).
    merge(realtime.issueCreated$, realtime.issueUpdated$, realtime.issueDeleted$)
      .pipe(debounceTime(300), takeUntilDestroyed(destroyRef))
      .subscribe(() => this.load());

    this.load();
  }

  load(): void {
    // Tabs only render once ProjectShellComponent has loaded the project, so the id is always set here.
    const projectId = this.projectId();
    const [sortBy, direction] = this.sort().split('-');
    this.loading.set(true);
    this.api.get<Page<Issue>>(`/projects/${projectId}/issues`, {
      search: this.search().trim(),
      status: this.status(),
      priority: this.priority(),
      assigneeId: this.assigneeId(),
      sortBy,
      descending: direction === 'desc',
      page: this.pageNumber(),
      pageSize: PAGE_SIZE,
    }).subscribe({
      next: page => {
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  reset(): void {
    this.pageNumber.set(1);
    this.load();
  }

  go(page: number): void {
    this.pageNumber.set(page);
    this.load();
  }

  clearFilters(): void {
    this.search.set('');
    this.status.set(null);
    this.priority.set(null);
    this.assigneeId.set(null);
    this.reset();
  }

  open(issue: Issue): void {
    void this.router.navigate(['/projects', issue.projectId, 'issues', issue.id]);
  }

  onCreated(issue: Issue): void {
    this.showCreate.set(false);
    this.toasts.success(`${this.key()}-${issue.issueNumber} created.`);
    this.reset();
  }
}
