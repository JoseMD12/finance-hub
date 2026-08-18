export interface ProblemDetails {
  readonly type?: string;
  readonly title: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errorCode?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly problemDetails: ProblemDetails;
  readonly status: number;
  readonly errorCode?: string;

  constructor(problem: ProblemDetails) {
    super(problem.detail || problem.title || 'Ocorreu um erro na requisição.');
    this.name = 'ApiError';
    this.problemDetails = problem;
    this.status = problem.status;
    this.errorCode = problem.errorCode;
  }
}
