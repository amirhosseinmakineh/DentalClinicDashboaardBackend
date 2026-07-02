export interface PaginatedResult<T> {
  items: readonly T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export function createPaginatedResult<T>(items: readonly T[], pageNumber: number, pageSize: number): PaginatedResult<T> {
  const safePageSize = Math.max(1, pageSize);
  const totalCount = items.length;
  const totalPages = Math.ceil(totalCount / safePageSize);
  const normalizedPageNumber = Math.min(Math.max(1, pageNumber), Math.max(1, totalPages));
  const startIndex = (normalizedPageNumber - 1) * safePageSize;

  return {
    items: items.slice(startIndex, startIndex + safePageSize),
    totalCount,
    pageNumber: normalizedPageNumber,
    pageSize: safePageSize,
    totalPages,
    hasPrevious: normalizedPageNumber > 1,
    hasNext: normalizedPageNumber < totalPages
  };
}
