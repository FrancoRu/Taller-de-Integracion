import { InputAdornment, Stack, TextField } from '@mui/material';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import type { TeamsSearchFilters } from '@/views/team/teams.types';

interface TeamsFilterBarProps {
  filters: TeamsSearchFilters;
  onFilterChange: (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => void;
}

const TeamsFilterBar: React.FC<TeamsFilterBarProps> = ({
  filters,
  onFilterChange,
}) => (
  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{
    mb: 2
  }}>
    <TextField
      label="Nombre"
      name="name"
      size="small"
      value={filters.name ?? ''}
      onChange={onFilterChange}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }
      }}
    />
    <TextField
      label="Código"
      name="threeLetterCode"
      size="small"
      value={filters.threeLetterCode ?? ''}
      onChange={onFilterChange}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }
      }}
    />
    <TextField
      label="Color camiseta"
      name="shirtColor"
      size="small"
      value={filters.shirtColor ?? ''}
      onChange={onFilterChange}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }
      }}
    />
  </Stack>
);

export default TeamsFilterBar;
