import { AuditLogFiltered } from '@/modules/auditLog/type/auditLog';

export const auditLogKeys = {
  list: (filter?: AuditLogFiltered) =>
    filter === undefined
      ? (['auditLog', 'list'] as const)
      : (['auditLog', 'list', filter] as const),
};
