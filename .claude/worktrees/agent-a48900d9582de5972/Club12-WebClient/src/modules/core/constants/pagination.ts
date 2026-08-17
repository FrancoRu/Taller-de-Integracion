import { Filtered } from '@/modules/core/types/types';

export const TABLE_ROWS_PER_PAGE = 10;
export const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50] as const;

/**
 * Page size used when fetching the full list of tournaments/divisions to
 * populate a filter dropdown, effectively treating the fetch as "get all".
 */
export const FILTER_OPTIONS_PAGE_SIZE = 300;

/**
 * Page size used on public (unauthenticated) listing pages that fetch
 * effectively all items in a single request.
 */
export const PUBLIC_LISTING_PAGE_SIZE = 100;

export const withTablePageSize = <T extends Filtered>(
  filter: T
): T & { pageSize: number } => ({
  ...filter,
  pageSize: filter.pageSize ?? TABLE_ROWS_PER_PAGE,
});
