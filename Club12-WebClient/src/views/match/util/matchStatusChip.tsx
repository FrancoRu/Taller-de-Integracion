import React, { useEffect, useState } from 'react';
import { Chip } from '@mui/material';
import dayjs from 'dayjs';
import duration from 'dayjs/plugin/duration';
import { IMatchStatusChipProps } from '@/modules/match/type/match';

dayjs.extend(duration);

export const MatchStatusChip: React.FC<IMatchStatusChipProps> = ({
  startTime,
  isFinished,
  maxMinutes = 120,
}) => {
  const [now, setNow] = useState(dayjs());

  useEffect(() => {
    const interval = setInterval(() => setNow(dayjs()), 1000);
    return () => clearInterval(interval);
  }, []);

  const start = dayjs(startTime);
  const end = start.add(maxMinutes, 'minute');

  if (isFinished || now.isAfter(end)) {
    return (
      <Chip label="Finalizado" variant="filled" size="small" color="error" />
    );
  }

  if (now.isBefore(start)) {
    const diff = dayjs.duration(start.diff(now));
    const timeString = `${String(diff.hours()).padStart(2, '0')}:${String(diff.minutes()).padStart(2, '0')}:${String(diff.seconds()).padStart(2, '0')}`;
    return (
      <Chip
        label={`Partido comenzará en ${timeString}`}
        color="success"
        variant="filled"
        size="small"
      />
    );
  }

  return <Chip label="En juego" color="success" />;
};
