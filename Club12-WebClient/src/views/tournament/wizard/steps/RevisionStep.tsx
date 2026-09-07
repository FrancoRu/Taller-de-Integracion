import { Alert, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import { WizardTreeNode } from '../wizardLogic';
import TreeNodeList from '../TreeNodeList';

interface RevisionStepProps {
  nodes: WizardTreeNode[];
  /**
   * HU-121: non-blocking, advisory warnings (e.g. an invalid sub-group
   * count) shown here — the organizer can still confirm and create the
   * tournament with them. Real team-count balance is checked later, once
   * teams are actually enrolled (`TournamentCompletabilityValidator`).
   */
  warnings?: string[];
}

/**
 * Read-only tree preview of everything the wizard is about to create.
 * Nothing is persisted until the admin confirms from here.
 */
export default function RevisionStep({ nodes, warnings = [] }: RevisionStepProps) {
  return (
    <Stack spacing={0.5}>
      <Typography
        variant="body2"
        sx={{
          color: "text.secondary",
          mb: 1
        }}>
        Nada se guarda todavía — esto es una vista previa de lo que se va a crear al confirmar.
      </Typography>
      {warnings.length > 0 && (
        <Alert severity="warning" sx={{ mb: 1 }}>
          <List dense disablePadding>
            {warnings.map(warning => (
              <ListItem key={warning} disableGutters>
                <ListItemText primary={warning} />
              </ListItem>
            ))}
          </List>
        </Alert>
      )}
      <TreeNodeList nodes={nodes} />
    </Stack>
  );
}
