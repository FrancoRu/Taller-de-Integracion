import { IStageResponse } from '@/modules/stage/type/stage.d';
import { translateStageType } from '@/modules/core/utils/translateStageType';

export const stageLabel = (stage: IStageResponse): string => {
  const typeLabel = translateStageType(stage.stageType);
  return stage.bracketName ? `${stage.bracketName} — ${typeLabel}` : typeLabel;
};
