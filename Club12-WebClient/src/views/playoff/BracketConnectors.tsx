import { RefObject, useLayoutEffect, useState } from 'react';
import { BracketEdge } from '@/modules/playoff/type/bracket.d';
import { GUID } from '@/modules/core/types/types';
import theme from '@/theme';

interface BracketConnectorsProps {
  edges: BracketEdge[];
  containerRef: RefObject<HTMLDivElement | null>;
  nodeRefs: RefObject<Map<GUID, HTMLDivElement | null>>;
}

interface ConnectorPath {
  id: string;
  d: string;
}

const buildPath = (fromRect: DOMRect, toRect: DOMRect, containerRect: DOMRect): string => {
  const startX = fromRect.right - containerRect.left;
  const startY = fromRect.top + fromRect.height / 2 - containerRect.top;
  const endX = toRect.left - containerRect.left;
  const endY = toRect.top + toRect.height / 2 - containerRect.top;
  const midX = startX + (endX - startX) / 2;

  return `M ${startX} ${startY} C ${midX} ${startY}, ${midX} ${endY}, ${endX} ${endY}`;
};

/**
 * SVG overlay that draws connector lines strictly from `edges` — the
 * caller (buildBracket) already omits any ambiguous/unresolved connector,
 * so this component never guesses: it only renders what it is given.
 */
export default function BracketConnectors({
  edges,
  containerRef,
  nodeRefs,
}: BracketConnectorsProps) {
  const [paths, setPaths] = useState<ConnectorPath[]>([]);

  useLayoutEffect(() => {
    const container = containerRef.current;
    if (!container || edges.length === 0) {
      setPaths([]);
      return undefined;
    }

    const compute = () => {
      const containerRect = container.getBoundingClientRect();
      const nextPaths: ConnectorPath[] = [];

      edges.forEach(edge => {
        const fromNode = nodeRefs.current.get(edge.fromMatchId);
        const toNode = nodeRefs.current.get(edge.toMatchId);
        if (!fromNode || !toNode) return;

        nextPaths.push({
          id: `${edge.fromMatchId}-${edge.toMatchId}`,
          d: buildPath(
            fromNode.getBoundingClientRect(),
            toNode.getBoundingClientRect(),
            containerRect
          ),
        });
      });

      setPaths(nextPaths);
    };

    compute();

    const resizeObserver = new ResizeObserver(compute);
    resizeObserver.observe(container);

    return () => resizeObserver.disconnect();
  }, [edges, containerRef, nodeRefs]);

  if (paths.length === 0) return null;

  return (
    <svg
      data-testid="bracket-connectors"
      style={{
        position: 'absolute',
        inset: 0,
        width: '100%',
        height: '100%',
        pointerEvents: 'none',
      }}
    >
      {paths.map(path => (
        <path
          key={path.id}
          d={path.d}
          fill="none"
          stroke={theme.palette.primary.main}
          strokeWidth={2}
        />
      ))}
    </svg>
  );
}
