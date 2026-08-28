import { useContext } from 'react';
import { MedicalRecordContext } from '@/modules/medicalRecord/context/medicalRecord.context';

export const useMedicalRecord = () => {
  const context = useContext(MedicalRecordContext);
  if (!context) {
    throw new Error(
      'useMedicalRecord must be used within a MedicalRecordProvider'
    );
  }
  return context;
};
