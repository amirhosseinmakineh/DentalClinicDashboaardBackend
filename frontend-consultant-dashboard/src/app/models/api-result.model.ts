export interface ApiResult<T = unknown> {
  isSuccess: boolean;
  message: string;
  data?: T | null;
}
