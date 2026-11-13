import { ChangeDetectionStrategy, Component, HostListener, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ISSUE_PRIORITIES, ISSUE_TYPES, Issue, IssuePriority, IssueType, ProjectMember } from '../../core/models';
import { FormErrors, toFormErrors } from '../../core/problem';

/** "New issue" dialog used by the issue list and the board. */
@Component({
  selector: 'app-issue-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="backdrop" (click)="closed.emit()">
      <form class="dialog" (click)="$event.stopPropagation()" (ngSubmit)="submit()" #form="ngForm" aria-label="New issue">
        <h2>New issue</h2>
        @if (errors().message) { <div class="alert error">{{ errors().message }}</div> }
        <div class="field">
          <label for="issue-title">Title</label>
          <input id="issue-title" class="input" name="title" required maxlength="300" placeholder="What needs to be done?" [(ngModel)]="title" autofocus />
          @for (message of errors().fields['title']; track message) { <span class="field-error">{{ message }}</span> }
        </div>
        <div class="field">
          <label for="issue-description">Description <span class="subtle">(optional)</span></label>
          <textarea id="issue-description" class="textarea" name="description" maxlength="5000" [(ngModel)]="description"></textarea>
        </div>
        <div class="form-grid">
          <div class="field">
            <label for="issue-type">Type</label>
            <select id="issue-type" class="select" name="type" [(ngModel)]="type">
              @for (option of types; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
            </select>
          </div>
          <div class="field">
            <label for="issue-priority">Priority</label>
            <select id="issue-priority" class="select" name="priority" [(ngModel)]="priority">
              @for (option of priorities; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
            </select>
          </div>
          <div class="field">
            <label for="issue-assignee">Assignee</label>
            <select id="issue-assignee" class="select" name="assignee" [(ngModel)]="assigneeId">
              <option [ngValue]="null">Unassigned</option>
              @for (member of members(); track member.userId) { <option [ngValue]="member.userId">{{ member.fullName }}</option> }
            </select>
          </div>
        </div>
        <div class="form-actions">
          <button class="btn secondary" type="button" (click)="closed.emit()">Cancel</button>
          <button class="btn" type="submit" [disabled]="busy() || form.invalid">Create issue</button>
        </div>
      </form>
    </div>
  `,
})
export class IssueFormDialogComponent {
  private readonly api = inject(ApiService);

  readonly projectId = input.required<string>();
  readonly members = input<ProjectMember[]>([]);
  readonly created = output<Issue>();
  readonly closed = output<void>();

  readonly types = ISSUE_TYPES;
  readonly priorities = ISSUE_PRIORITIES;
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });
  title = '';
  description = '';
  type = IssueType.Task;
  priority = IssuePriority.Medium;
  assigneeId: string | null = null;

  submit(): void {
    this.busy.set(true);
    const body = { title: this.title.trim(), description: this.description.trim() || null, type: this.type, priority: this.priority, assigneeId: this.assigneeId };
    this.api.post<Issue>(`/projects/${this.projectId()}/issues`, body).subscribe({
      next: issue => this.created.emit(issue),
      error: error => {
        this.errors.set(toFormErrors(error));
        this.busy.set(false);
      },
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closed.emit();
  }
}
