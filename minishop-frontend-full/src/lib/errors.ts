import axios, { AxiosError } from 'axios';

type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
};

export function extractErrorMessage(err: unknown): string {
  // Axios error?
  if (axios.isAxiosError(err)) {
    const ax = err as AxiosError<any>;
    const data = ax.response?.data as ProblemDetails | string | undefined;

    // If backend returned text, use it
    if (typeof data === 'string') return data;

    // If backend returned ProblemDetails
    if (data && typeof data === 'object') {
      // 1) ModelState errors
      if (data.errors && typeof data.errors === 'object') {
        const msgs: string[] = [];
        for (const [field, arr] of Object.entries(data.errors)) {
          if (Array.isArray(arr)) {
            arr.forEach(m => msgs.push(`${field}: ${m}`));
          }
        }
        if (msgs.length) return msgs.join('\n');
      }
      // 2) Title or detail
      if (data.detail) return data.detail;
      if (data.title) return data.title;
    }

    // Fallback to HTTP status or message
    if (ax.response?.status) {
      return `HTTP ${ax.response.status}: ${ax.message}`;
    }
    return ax.message || 'Request failed.';
  }

  // Non-Axios error
  if (err instanceof Error) return err.message;
  return String(err);
}
