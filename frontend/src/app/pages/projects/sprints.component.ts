import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ConfirmService } from '../../core/confirm.service';
import { BacklogIssue, ISSUE_PRIORITIES, ISSUE_STATUSES, SPRINT_STATUSES, Sprint, SprintStatus } from '../../core/models';
import { FormErrors, errorMessage, toFormErrors } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { LabelPipe, ShortDatePipe } from '../../shared/pipes';
import { ProjectContext } from './project-context';

interface SprintDraft { id?: string; name: string; goal: string; start: string; end: string; }

@Component({
  selector: 'app-sprints',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, LabelPipe, ShortDatePipe],
  template: `
    <div class="row between" style="margin-bottom: 16px">
      <p class="muted" style="margin: 0">Plan work into time-boxed sprints. Only one sprint can be active at a time.</p>
      @if (canManage()) { <button class="btn" type="button" (click)="openEditor()">+ New sprint</button> }
    </div>

    <div class="stack">
      @for (sprint of orderedSprints(); track sprint.id) {
        <div class="card">
          <div class="card-header" style="align-items: flex-start">
            <div>
              <div class="row">
                <h3 style="margin: 0">{{ sprint.name }}</h3>
                <span class="badge" [class]="'badge sprint-' + sprint.status">{{ sprint.status | label: sprintStatuses }}</span>
                <span class="subtle">{{ sprint.issueCount }} issue{{ sprint.issueCount === 1 ? '' : 's' }}</span>
              </div>
              <p class="muted" style="margin: 4px 0 0">{{ sprint.goal || 'No goal set' }}</p>
              <span class="subtle">{{ sprint.startDateUtc | shortDate: 'No start date' }} → {{ sprint.endDateUtc | shortDate: 'No end date' }}</span>
            </div>
            <div class="row">
              <button class="btn secondary sm" type="button" (click)="toggle(sprint)">{{ expanded() === sprint.id ? 'Hide issues' : 'Issues' }}</button>
              @if (canManage()) {
                @if (sprint.status === planned) {
                  <button class="btn sm" type="button" (click)="start(sprint)" [disabled]="!!activeSprint()" [attr.title]="activeSprint() ? 'Complete the active sprint first' : null">Start</button>
                  <button class="btn ghost sm" type="button" (click)="openEditor(sprint)">Edit</button>
                }
                @if (sprint.status === active) {
                  <button class="btn sm" type="button" (click)="complete(sprint)">Complete</button>
                }
                @if (sprint.status !== active) {
                  <button class="btn danger-ghost sm" type="button" (click)="remove(sprint)">Delete</button>
                }
              }
            </div>
          </div>
          @if (expanded() === sprint.id) {
            <ul class="list">
              @for (issue of sprintIssues(); track issue.id) {
                <li>
                  <span class="num mono subtle">{{ key() }}-{{ issue.issueNumber }}</span>
                  <a class="person-name" [routerLink]="['../issues', issue.id]">{{ issue.title }}</a>
                  <span class="spacer"></span>
                  <span class="badge" [class]="'badge status-' + issue.status">{{ issue.status | label: issueStatuses }}</span>
                  @if (canContribute() && sprint.status !== completed) {
                    <button class="btn ghost sm" type="button" (click)="removeFromSprint(sprint, issue)">Move to backlog</button>
                  }
                </li>
              } @empty {
                <li class="muted">No issues in this sprint yet.</li>
              }
            </ul>
          }
        </div>
      } @empty {
        <div class="card empty">
          <h3>No sprints yet</h3>
          <p>{{ canManage() ? 'Create a sprint to start planning.' : 'A project manager can create sprints.' }}</p>
        </div>
      }

      <div class="card">
        <div class="card-header"><h2>Backlog</h2><span class="subtle">{{ backlog().length }} issue{{ backlog().length === 1 ? '' : 's' }}</span></div>
        <ul class="list">
          @for (issue of backlog(); track issue.id) {
            <li>
              <span class="num mono subtle">{{ key() }}-{{ issue.issueNumber }}</span>
              <a class="person-name" [routerLink]="['../issues', issue.id]">{{ issue.title }}</a>
              <span class="spacer"></span>
              <span class="badge hide-mobile" [class]="'badge priority-' + issue.priority">{{ issue.priority | label: priorities }}</span>
              @if (canContribute() && openSprints().length) {
                <select class="select" style="width: auto; min-height: 30px" [attr.aria-label]="'Add ' + issue.title + ' to sprint'"
                        [ngModel]="''" (ngModelChange)="addToSprint($event, issue)">
                  <option value="" disabled>Add to sprint…</option>
                  @for (sprint of openSprints(); track sprint.id) { <option [value]="sprint.id">{{ sprint.name }}</option> }
                </select>
              }
            </li>
          } @empty {
            <li class="muted">The backlog is empty. 🎉</li>
          }
        </ul>
      </div>
    </div>

    @if (draft(); as editor) {
      <div class="backdrop" (click)="draft.set(null)">
        <form class="dialog" (click)="$event.stopPropagation()" (ngSubmit)="save(editor)" #form="ngForm" [attr.aria-label]="editor.id ? 'Edit sprint' : 'New sprint'">
          <h2>{{ editor.id ? 'Edit sprint' : 'New sprint' }}</h2>
          @if (errors().message) { <div class="alert error">{{ errors().message }}</div> }
          <div class="field">
            <label for="sprint-name">Name</label>
            <input id="sprint-name" class="input" name="name" required maxlength="200" [(ngModel)]="editor.name" />
          </div>
          <div class="field">
            <label for="sprint-goal">Goal <span class="subtle">(optional)</span></label>
            <textarea id="sprint-goal" class="textarea" name="goal" maxlength="1000" [(ngModel)]="editor.goal"></textarea>
          </div>
          <div class="form-grid">
            <div class="field"><label for="sprint-start">Start date</label><input id="sprint-start" class="input" type="date" name="start" [(ngModel)]="editor.start" /></div>
            <div class="field">
              <label for="sprint-end">End date</label>
              <input id="sprint-end" class="input" type="date" name="end" [min]="editor.start" [(ngModel)]="editor.end" [class.invalid]="datesInvalid(editor)" />
              @if (datesInvalid(editor)) { <span class="field-error">End date must be on or after the start date.</span> }
            </div>
          </div>
          <div class="form-actions">
            <button class="btn secondary" type="button" (click)="draft.set(null)">Cancel</button>
            <button class="btn" type="submit" [disabled]="busy() || form.invalid || datesInvalid(editor)">{{ editor.id ? 'Save' : 'Create sprint' }}</button>
          </div>
        </form>
      </div>
    }
  `,
})
export class SprintsComponent {
  readonly context = inject(ProjectContext);
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toasts = inject(ToastService);

  readonly sprintStatuses = SPRINT_STATUSES;
  readonly issueStatuses = ISSUE_STATUSES;
  readonly priorities = ISSUE_PRIORITIES;
  readonly planned = SprintStatus.Planned;
  readonly active = SprintStatus.Active;
  readonly completed = SprintStatus.Completed;

  readonly sprints = signal<Sprint[]>([]);
  readonly backlog = signal<BacklogIssue[]>([]);
  readonly sprintIssues = signal<BacklogIssue[]>([]);
  readonly expanded = signal<string | null>(null);
  readonly draft = signal<SprintDraft | null>(null);
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });

  readonly projectId = computed(() => this.context.project()?.id ?? '');
  readonly key = computed(() => this.context.project()?.key ?? '');
  readonly canManage = computed(() => this.context.permissions().canManage);
  readonly canContribute = computed(() => this.context.permissions().canContribute);
  readonly activeSprint = computed(() => this.sprints().find(sprint => sprint.status === SprintStatus.Active) ?? null);
  readonly openSprints = computed(() => this.sprints().filter(sprint => sprint.status === SprintStatus.Planned || sprint.status === SprintStatus.Active));
  readonly orderedSprints = computed(() => {
    const order: Record<number, number> = { [SprintStatus.Active]: 0, [SprintStatus.Planned]: 1, [SprintStatus.Completed]: 2, [SprintStatus.Cancelled]: 3 };
    return [...this.sprints()].sort((a, b) => order[a.status] - order[b.status]);
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.api.get<Sprint[]>(`/projects/${this.projectId()}/sprints`).subscribe(items => this.sprints.set(items));
    this.api.get<BacklogIssue[]>(`/projects/${this.projectId()}/backlog`).subscribe(items => this.backlog.set(items));
    const expanded = this.expanded();
    if (expanded) this.loadSprintIssues(expanded);
  }

  toggle(sprint: Sprint): void {
    if (this.expanded() === sprint.id) {
      this.expanded.set(null);
      return;
    }
    this.expanded.set(sprint.id);
    this.sprintIssues.set([]);
    this.loadSprintIssues(sprint.id);
  }

  openEditor(sprint?: Sprint): void {
    this.errors.set({ message: '', fields: {} });
    this.draft.set({
      id: sprint?.id,
      name: sprint?.name ?? `Sprint ${this.sprints().length + 1}`,
      goal: sprint?.goal ?? '',
      start: sprint?.startDateUtc?.slice(0, 10) ?? '',
      end: sprint?.endDateUtc?.slice(0, 10) ?? '',
    });
  }

  datesInvalid(draft: SprintDraft): boolean {
    return !!draft.start && !!draft.end && draft.end < draft.start;
  }

  save(draft: SprintDraft): void {
    const body = {
      name: draft.name.trim(),
      goal: draft.goal.trim() || null,
      startDateUtc: draft.start ? `${draft.start}T00:00:00Z` : null,
      endDateUtc: draft.end ? `${draft.end}T00:00:00Z` : null,
    };
    const request: Observable<unknown> = draft.id
      ? this.api.put<void>(`/sprints/${draft.id}`, body)
      : this.api.post<Sprint>(`/projects/${this.projectId()}/sprints`, body);

    this.busy.set(true);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.draft.set(null);
        this.toasts.success(draft.id ? 'Sprint updated.' : 'Sprint created.');
        this.load();
      },
      error: error => {
        this.busy.set(false);
        this.errors.set(toFormErrors(error));
      },
    });
  }

  start(sprint: Sprint): void {
    this.api.post<void>(`/sprints/${sprint.id}/start`).subscribe({
      next: () => {
        this.toasts.success(`${sprint.name} started.`);
        this.load();
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  async complete(sprint: Sprint): Promise<void> {
    if (!(await this.confirm.ask({ title: `Complete ${sprint.name}?`, message: 'The sprint will be closed. Unfinished issues stay assigned to it.', confirmLabel: 'Complete sprint' }))) return;
    this.api.post<void>(`/sprints/${sprint.id}/complete`).subscribe({
      next: () => {
        this.toasts.success(`${sprint.name} completed.`);
        this.load();
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  async remove(sprint: Sprint): Promise<void> {
    if (!(await this.confirm.ask({ title: `Delete ${sprint.name}?`, message: 'Its issues will be moved back to the backlog.', confirmLabel: 'Delete sprint', danger: true }))) return;
    this.api.delete<void>(`/sprints/${sprint.id}`).subscribe({
      next: () => {
        if (this.expanded() === sprint.id) this.expanded.set(null);
        this.load();
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  addToSprint(sprintId: string, issue: BacklogIssue): void {
    if (!sprintId) return;
    this.api.put<void>(`/sprints/${sprintId}/issues/${issue.id}`).subscribe({
      next: () => this.load(),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  removeFromSprint(sprint: Sprint, issue: BacklogIssue): void {
    this.api.delete<void>(`/sprints/${sprint.id}/issues/${issue.id}`).subscribe({
      next: () => this.load(),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  private loadSprintIssues(sprintId: string): void {
    this.api.get<BacklogIssue[]>(`/sprints/${sprintId}/issues`).subscribe(items => this.sprintIssues.set(items));
  }
}
