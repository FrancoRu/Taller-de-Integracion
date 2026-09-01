import { Box, Divider, Paper, Stack, Typography } from '@mui/material';
import { ITeamStaffResponse } from '@/modules/teamStaff/type/teamStaff';
import { TEAM_STAFF_ROLE_LABEL } from '@/modules/teamStaff/utils/teamStaffDisplay';
import SectionHeading from '@/views/core/components/SectionHeading';

interface TeamStaffSectionProps {
  /** The team's technical staff for the currently selected tournament. */
  staff: ITeamStaffResponse[];
}

/**
 * Public, read-only "Cuerpo técnico" section on a team's profile: name and
 * Spanish role label for each technical staff member. A supplementary
 * section, not a primary one — renders nothing (not an empty-state message)
 * when the team has no staff for the selected tournament.
 */
const TeamStaffSection: React.FC<TeamStaffSectionProps> = ({ staff }) => {
  if (staff.length === 0) {
    return null;
  }

  return (
    <Box component="section" sx={{ mb: 4 }}>
      <SectionHeading>Cuerpo técnico</SectionHeading>
      <Paper variant="outlined">
        <Stack divider={<Divider />}>
          {staff.map(member => (
            <Stack
              key={member.id}
              direction="row"
              spacing={2}
              sx={{ alignItems: 'center', px: 2, py: 1.25 }}
            >
              <Typography variant="body2" noWrap sx={{ fontWeight: 500, flex: 1 }}>
                {member.fullName}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                {TEAM_STAFF_ROLE_LABEL[member.role]}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Paper>
    </Box>
  );
};

export default TeamStaffSection;
