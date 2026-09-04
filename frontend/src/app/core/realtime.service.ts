import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from './environment';

export interface IssueEventDto { id: string; projectId: string; issueNumber: number; title: string; description?: string; type: number; status: number; priority: number; reporterId: string; assigneeId?: string; createdAtUtc: string; updatedAtUtc?: string; }
export interface CommentEventDto { id: string; issueId: string; authorId: string; content: string; createdAtUtc: string; updatedAtUtc?: string; }
export interface CommentDeletedEventDto { issueId: string; commentId: string; }
export interface NotificationEventDto { id: string; title: string; message: string; isRead: boolean; createdAtUtc: string; }

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private connection?: HubConnection;
  private readonly joinedProjects = new Set<string>();

  readonly issueCreated$ = new Subject<IssueEventDto>();
  readonly issueUpdated$ = new Subject<IssueEventDto>();
  readonly issueDeleted$ = new Subject<string>();
  readonly commentAdded$ = new Subject<CommentEventDto>();
  readonly commentDeleted$ = new Subject<CommentDeletedEventDto>();
  readonly notificationCreated$ = new Subject<NotificationEventDto>();

  private ensureConnection(): HubConnection {
    if (this.connection) return this.connection;
    const connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('issueCreated', (issue: IssueEventDto) => this.issueCreated$.next(issue));
    connection.on('issueUpdated', (issue: IssueEventDto) => this.issueUpdated$.next(issue));
    connection.on('issueDeleted', (issueId: string) => this.issueDeleted$.next(issueId));
    connection.on('commentAdded', (comment: CommentEventDto) => this.commentAdded$.next(comment));
    connection.on('commentDeleted', (payload: CommentDeletedEventDto) => this.commentDeleted$.next(payload));
    connection.on('notificationCreated', (notification: NotificationEventDto) => this.notificationCreated$.next(notification));

    connection.onreconnected(() => { for (const projectId of this.joinedProjects) void connection.invoke('JoinProject', projectId); });

    this.connection = connection;
    return connection;
  }

  private async start(): Promise<HubConnection> {
    const connection = this.ensureConnection();
    if (connection.state === HubConnectionState.Disconnected) await connection.start();
    return connection;
  }

  async connect(): Promise<void> {
    if (!this.auth.isAuthenticated()) return;
    await this.start();
  }

  async joinProject(projectId: string): Promise<void> {
    if (!this.auth.isAuthenticated() || !projectId) return;
    const connection = await this.start();
    this.joinedProjects.add(projectId);
    await connection.invoke('JoinProject', projectId);
  }

  async leaveProject(projectId: string): Promise<void> {
    this.joinedProjects.delete(projectId);
    if (this.connection?.state === HubConnectionState.Connected) await this.connection.invoke('LeaveProject', projectId);
  }

  async stop(): Promise<void> {
    this.joinedProjects.clear();
    await this.connection?.stop();
  }
}
