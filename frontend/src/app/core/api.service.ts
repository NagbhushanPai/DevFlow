import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}
  get<T>(url: string) { return this.http.get<T>(`/api${url}`); }
  post<T>(url: string, body: unknown) { return this.http.post<T>(`/api${url}`, body); }
  put<T>(url: string, body: unknown) { return this.http.put<T>(`/api${url}`, body); }
}
