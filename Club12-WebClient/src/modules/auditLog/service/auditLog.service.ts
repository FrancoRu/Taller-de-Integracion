import { AxiosResponse } from 'axios';
import routes from '@/modules/core/constants/routes';
import { withTablePageSize } from '@/modules/core/constants/pagination';
import { GenericResponsePagination } from '@/modules/core/types/types';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import {
  AuditLogFiltered,
  IAuditLogResponse,
} from '@/modules/auditLog/type/auditLog';

/**
 * Read-only service for the sensitive-action audit trail (HU-101).
 */
export const auditLogService = {
  /**
   * Fetches audit entries (newest first) with pagination and optional filters.
   * @param {AuditLogFiltered} filter - The filter criteria to apply.
   * @returns {Promise<AxiosResponse<GenericResponsePagination<IAuditLogResponse>>>}
   */
  getAuditLogs: async (
    filter: AuditLogFiltered
  ): Promise<AxiosResponse<GenericResponsePagination<IAuditLogResponse>>> =>
    sendGet<GenericResponsePagination<IAuditLogResponse>>(
      routes.auditLogs,
      withTablePageSize(filter)
    ),
};
