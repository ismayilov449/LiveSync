import type { ApiError } from '../types';

export class HttpError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = 'HttpError';
  }
}

async function parseError(response: Response): Promise<string> {
  try {
    const body = await response.json();
    if (typeof body === 'string') return body;
    if (Array.isArray(body)) return body.join(', ');
    if (body && typeof body === 'object') {
      const err = body as ApiError & { title?: string; detail?: string };
      return err.detail ?? err.title ?? err.message ?? response.statusText;
    }
  } catch {
    // ignore
  }
  return response.statusText || 'Request failed';
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string | null,
): Promise<T> {
  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`);
  }

  const response = await fetch(path, { ...options, headers });

  if (!response.ok) {
    throw new HttpError(await parseError(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) return undefined as T;

  const contentType = response.headers.get('Content-Type') ?? '';
  if (contentType.includes('text/html') || text.trimStart().startsWith('<!')) {
    throw new HttpError(
      'Server returned HTML instead of JSON. Is the API running and is the route correct?',
      response.status,
    );
  }

  return JSON.parse(text) as T;
}
