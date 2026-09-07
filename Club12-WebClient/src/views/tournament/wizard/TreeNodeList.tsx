import { Chip, Stack, Typography } from '@mui/material';
import { WizardTreeNode } from './wizardLogic';

interface TreeNodeListProps {
  nodes: WizardTreeNode[];
}

const INDENT_BY_DEPTH: Record<WizardTreeNode['depth'], number> = {
  1: 0,
  2: 2,
  3: 4,
};

/**
 * Renders a flat {@link WizardTreeNode} list as an indented tree — the
 * wizard's review step and any other read-only structure preview (e.g. a
 * division's own "how this is organized" view) share this exact rendering.
 */
export default function TreeNodeList({ nodes }: TreeNodeListProps) {
  return (
    <Stack spacing={0.5}>
      {nodes.map(node => (
        <Stack
          key={node.id}
          direction="row"
          spacing={1}
          sx={{
            alignItems: 'center',
            pl: INDENT_BY_DEPTH[node.depth],
          }}
        >
          <Typography
            variant={node.depth === 1 ? 'subtitle1' : 'body2'}
            color={node.depth === 3 ? 'text.secondary' : 'text.primary'}
            sx={{
              fontWeight: node.depth === 1 ? 700 : node.depth === 2 ? 600 : 400,
            }}
          >
            {node.label}
          </Typography>
          {node.tag && <Chip size="small" label={node.tag} />}
        </Stack>
      ))}
    </Stack>
  );
}
