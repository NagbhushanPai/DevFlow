import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from './environment';
import { Comment, Issue, NotificationItem } from './models';

export interface CommentDeletedEvent { issueId: string; commentId: string; }
export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/** SignalR connection to /hubs/issues. Tokens are fetched fresh (refreshing if needed) for every (re)connect. */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);
  private connection?: HubConnection;
  private starting?: Promise<HubConnection>;
  private readonly joinedProjects = new Set<string>();

  readonly status = signal<ConnectionStatus>('disconnected');
  readonly issueCreated$ = new Subject<Issue>();
  readonly issueUpdated$ = new Subject<Issue>();
  readonly issueDeleted$ = new Subject<string>();
  readonly commentAdded$ = new Subject<Comment>();
  readonly commentDeleted$ = new Subject<CommentDeletedEvent>();
  readonly notificationCreated$ = new Subject<NotificationItem>();

  async connect(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      return;
    }

    try {
      await this.start();
    } catch {
      // The app works without realtime updates; automatic reconnect handles transient failures.
      this.status.set('disconnected');
    }
  }

  async joinProject(projectId: string): Promise<void> {
    if (!projectId || !this.auth.isAuthenticated()) {
      return;
    }

    this.joinedProjects.add(projectId);
    try {
      const connection = await this.start();
      await connection.invoke('JoinProject', projectId);
    } catch {
      // Ignored: the page still loads data over HTTP.
    }
  }

  async leaveProject(projectId: string): Promise<void> {
    this.joinedProjects.delete(projectId);
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveProject', projectId).catch(() => undefined);
    }
  }

  async stop(): Promise<void> {
    this.joinedProjects.clear();
    await this.connection?.stop();
    this.status.set('disconnected');
  }

  /**
   * Starts the connection once. Concurrent callers (the app shell connecting and a page joining a project at the
   * same time) share the same start promise; calling HubConnection.start() twice would throw and the second
   * caller would silently never join its project group.
   */
  private start(): Promise<HubConnection> {
    const connection = this.ensureConnection();
    if (connection.state === HubConnectionState.Connected) {
      return Promise.resolve(connection);
    }

    this.starting ??= (async () => {
      this.status.set('connecting');
      try {
        if (connection.state === HubConnectionState.Disconnected) {
          await connection.start();
        }
        this.status.set('connected');
        return connection;
      } catch (error) {
        this.status.set('disconnected');
        throw error;
      } finally {
        this.starting = undefined;
      }
    })();

    return this.starting;
  }

  private ensureConnection(): HubConnection {
    if (this.connection) {
      return this.connection;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.freshToken() })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('issueCreated', (issue: Issue) => this.issueCreated$.next(issue));
    connection.on('issueUpdated', (issue: Issue) => this.issueUpdated$.next(issue));
    connection.on('issueDeleted', (issueId: string) => this.issueDeleted$.next(issueId));
    connection.on('commentAdded', (comment: Comment) => this.commentAdded$.next(comment));
    connection.on('commentDeleted', (payload: CommentDeletedEvent) => this.commentDeleted$.next(payload));
    connection.on('notificationCreated', (notification: NotificationItem) => this.notificationCreated$.next(notification));

    connection.onreconnecting(() => this.status.set('reconnecting'));
    connection.onreconnected(() => {
      this.status.set('connected');
      for (const projectId of this.joinedProjects) {
        void connection.invoke('JoinProject', projectId).catch(() => undefined);
      }
    });
    connection.onclose(() => this.status.set('disconnected'));

    this.connection = connection;
    return connection;
  }
}
