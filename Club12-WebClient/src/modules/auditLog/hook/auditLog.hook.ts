import { useContext } from 'react';
import { AuditLogContext } from '@/modules/auditLog/context/auditLog.context';

export const useAuditLog = () => {
  const context = useContext(AuditLogContext);
  if (!context) {
    throw new Error('useAuditLog must be used within an AuditLogProvider');
  }
  return context;
};
