import { useEffect, useMemo, useRef } from 'react';
import { ListSubheader, MenuItem, Stack, TextField } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { IDivisionResponse } from '@/modules/division/type/division';
import { stageLabel } from '@/modules/stage/utils/stageLabel';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

interface DivisionStagePickerProps {
  tournamentId: GUID | '';
  divisionId: GUID | '';
  stageId: GUID | '';
  onDivisionChange: (divisionId: GUID | '') => void;
  onStageChange: (stageId: GUID | '') => void;
  showStage?: boolean;
}

export default function DivisionStagePicker({
  tournamentId,
  divisionId,
  stageId,
  onDivisionChange,
  onStageChange,
  showStage = true,
}: DivisionStagePickerProps) {
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const getDivisionsRef = useRef(getDivisionsByFilters);
  const getStagesRef = useRef(getStagesByFilters);

  useEffect(() => {
    getDivisionsRef.current = getDivisionsByFilters;
  }, [getDivisionsByFilters]);

  useEffect(() => {
    getStagesRef.current = getStagesByFilters;
  }, [getStagesByFilters]);

  useEffect(() => {
    if (!tournamentId) return;
    void getDivisionsRef.current({ tournamentId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [tournamentId]);

  useEffect(() => {
    if (!divisionId || !showStage) return;
    void getStagesRef.current({ divisionId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [divisionId, showStage]);

  const [zones, cups] = useMemo(() => {
    const all: IDivisionResponse[] = divisions ?? [];
    return [all.filter(d => !d.isCrossDivisionCup), all.filter(d => d.isCrossDivisionCup)];
  }, [divisions]);

  const stageOptions = useMemo(() => stages ?? [], [stages]);

  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
      <TextField
        select
        label="División / Copa"
        size="small"
        value={divisionId}
        onChange={e => {
          onDivisionChange(e.target.value as GUID | '');
          onStageChange('');
        }}
        sx={{ minWidth: 220 }}
        disabled={!tournamentId}
      >
        <MenuItem value="">Todas</MenuItem>
        {zones.length > 0 && <ListSubheader>Divisiones</ListSubheader>}
        {zones.map(division => (
          <MenuItem key={division.id} value={division.id}>
            {division.name}
          </MenuItem>
        ))}
        {cups.length > 0 && <ListSubheader>Copa</ListSubheader>}
        {cups.map(division => (
          <MenuItem key={division.id} value={division.id}>
            {division.name}
          </MenuItem>
        ))}
      </TextField>

      {showStage && (
        <TextField
          select
          label="Fase"
          size="small"
          value={stageId}
          onChange={e => onStageChange(e.target.value as GUID | '')}
          sx={{ minWidth: 220 }}
          disabled={!divisionId}
        >
          <MenuItem value="">Todas</MenuItem>
          {stageOptions.map(stage => (
            <MenuItem key={stage.id} value={stage.id}>
              {stageLabel(stage)}
            </MenuItem>
          ))}
        </TextField>
      )}
    </Stack>
  );
}
