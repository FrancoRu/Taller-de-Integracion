import React, { createContext, ReactNode, useCallback, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { GenericResponsePagination } from '@/modules/core/types/types';
import { useUnknownErrorHandler } from '@/modules/error/hooks/useUnknownErrorHandler';
import { auditLogService } from '@/modules/auditLog/service/auditLog.service';
import {
  AuditLogFiltered,
  IAuditLogContextProps,
  IAuditLogResponse,
} from '@/modules/auditLog/type/auditLog';
import { auditLogKeys } from '@/modules/auditLog/queryKeys';

export const AuditLogContext = createContext<IAuditLogContextProps | undefined>(
  undefined
);

export const AuditLogProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const queryClient = useQueryClient();
  const handleUnknownError = useUnknownErrorHandler();

  const getAuditLogs = useCallback(
    async (
      filter: AuditLogFiltered
    ): Promise<GenericResponsePagination<IAuditLogResponse> | void> => {
      try {
        const response = await queryClient.fetchQuery({
          queryKey: auditLogKeys.list(filter),
          queryFn: async () => await auditLogService.getAuditLogs(filter),
        });

        return response?.data;
      } catch (error: unknown) {
        handleUnknownError(error);
      }
    },
    [queryClient, handleUnknownError]
  );

  const container: IAuditLogContextProps = useMemo(
    () => ({ getAuditLogs }),
    [getAuditLogs]
  );

  return (
    <AuditLogContext.Provider value={container}>
      {children}
    </AuditLogContext.Provider>
  );
};
