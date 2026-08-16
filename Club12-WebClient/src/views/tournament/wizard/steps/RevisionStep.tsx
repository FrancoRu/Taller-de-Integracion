import { Chip, Stack, Typography } from '@mui/material';
import { WizardTreeNode } from '../wizardLogic';

interface RevisionStepProps {
  nodes: WizardTreeNode[];
}

const INDENT_BY_DEPTH: Record<WizardTreeNode['depth'], number> = {
  1: 0,
  2: 2,
  3: 4,
};

/**
 * Read-only tree preview of everything the wizard is about to create.
 * Nothing is persisted until the admin confirms from here.
 */
export default function RevisionStep({ nodes }: RevisionStepProps) {
  return (
    <Stack spacing={0.5}>
      <Typography variant="body2" color="text.secondary" mb={1}>
        Nada se guarda todavía — esto es una vista previa de lo que se va a crear al confirmar.
      </Typography>
      {nodes.map(node => (
        <Stack
          key={node.id}
          direction="row"
          spacing={1}
          alignItems="center"
          sx={{ pl: INDENT_BY_DEPTH[node.depth] }}
        >
          <Typography
            variant={node.depth === 1 ? 'subtitle1' : 'body2'}
            fontWeight={node.depth === 1 ? 700 : node.depth === 2 ? 600 : 400}
            color={node.depth === 3 ? 'text.secondary' : 'text.primary'}
          >
            {node.label}
          </Typography>
          {node.tag && <Chip size="small" label={node.tag} />}
        </Stack>
      ))}
    </Stack>
  );
}
