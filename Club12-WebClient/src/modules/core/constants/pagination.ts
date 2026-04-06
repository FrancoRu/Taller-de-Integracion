import { Filtered } from '@/modules/core/types/types';

export const TABLE_ROWS_PER_PAGE = 10;
export const TABLE_PAGE_SIZE_OPTIONS = [10, 25, 50] as const;

export const withTablePageSize = <T extends Filtered>(
  filter: T
): T & { pageSize: number } => ({
  ...filter,
  pageSize: filter.pageSize ?? TABLE_ROWS_PER_PAGE,
});
