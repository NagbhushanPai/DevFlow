import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => next(request).pipe(catchError((error: HttpErrorResponse) => { if (error.status === 401) inject(AuthService).logout(); if (error.status === 0) console.error('DevFlow API is unavailable.'); return throwError(() => error); }));
