import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

type QueryValue = string | number | boolean | null | undefined;

/** Thin wrapper around HttpClient that prefixes /api and builds query strings safely. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  get<T>(url: string, query?: Record<string, QueryValue>): Observable<T> {
    return this.http.get<T>(`/api${url}`, { params: toParams(query) });
  }

  post<T>(url: string, body: unknown = {}): Observable<T> {
    return this.http.post<T>(`/api${url}`, body);
  }

  put<T>(url: string, body: unknown = {}): Observable<T> {
    return this.http.put<T>(`/api${url}`, body);
  }

  delete<T>(url: string): Observable<T> {
    return this.http.delete<T>(`/api${url}`);
  }

  /** Downloads an authenticated file (e.g. an attachment) and saves it with the given name. */
  download(url: string, fileName: string): Observable<void> {
    return new Observable<void>(subscriber => {
      const subscription = this.http.get(url.startsWith('/api') ? url : `/api${url}`, { responseType: 'blob' }).subscribe({
        next: blob => {
          const objectUrl = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = objectUrl;
          anchor.download = fileName;
          anchor.rel = 'noopener';
          document.body.appendChild(anchor);
          anchor.click();
          anchor.remove();
          setTimeout(() => URL.revokeObjectURL(objectUrl), 10_000);
          subscriber.next();
          subscriber.complete();
        },
        error: error => subscriber.error(error),
      });
      return () => subscription.unsubscribe();
    });
  }
}

function toParams(query?: Record<string, QueryValue>): HttpParams | undefined {
  if (!query) {
    return undefined;
  }

  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, String(value));
    }
  }

  return params;
}
