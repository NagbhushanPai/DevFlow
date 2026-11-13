import { HttpErrorResponse } from '@angular/common/http';

/** RFC 7807 problem details as produced by the API's GlobalExceptionHandler. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]> | string[];
  traceId?: string;
}

/** Field errors keyed by camelCase property name (e.g. "password"), plus a general message. */
export interface FormErrors {
  message: string;
  fields: Record<string, string[]>;
}

export function toFormErrors(error: unknown, fallback = 'Something went wrong. Please try again.'): FormErrors {
  if (!(error instanceof HttpErrorResponse)) {
    return { message: fallback, fields: {} };
  }

  if (error.status === 0) {
    return { message: 'Cannot reach the server. Check your connection and try again.', fields: {} };
  }

  const problem = (typeof error.error === 'object' && error.error ? error.error : {}) as ProblemDetails;
  const fields: Record<string, string[]> = {};
  let messages: string[] = [];

  if (Array.isArray(problem.errors)) {
    messages = problem.errors;
  } else if (problem.errors) {
    for (const [key, value] of Object.entries(problem.errors)) {
      fields[camelCase(key)] = value;
    }
  }

  if (error.status === 429) {
    return { message: 'Too many requests. Please wait a moment and try again.', fields };
  }

  const message = messages.join(' ')
    || problem.detail
    || (Object.keys(fields).length ? 'Please fix the highlighted fields.' : '')
    || (typeof error.error === 'string' && error.error.length < 300 ? error.error : '')
    || problem.title
    || fallback;

  return { message, fields };
}

export function errorMessage(error: unknown, fallback?: string): string {
  return toFormErrors(error, fallback).message;
}

function camelCase(key: string): string {
  const last = key.split('.').pop() ?? key;
  return last.charAt(0).toLowerCase() + last.slice(1);
}
