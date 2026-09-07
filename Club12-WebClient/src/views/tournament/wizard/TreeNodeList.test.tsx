import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { WizardTreeNode } from './wizardLogic';
import TreeNodeList from './TreeNodeList';

describe('TreeNodeList', () => {
  it('renders every node label, in order, with its optional tag', () => {
    const nodes: WizardTreeNode[] = [
      { id: 'a', depth: 1, label: 'Torneo de prueba' },
      { id: 'b', depth: 2, label: 'Zona A' },
      { id: 'c', depth: 3, label: 'Fase de grupos' },
      { id: 'd', depth: 2, label: 'Copa Club12', tag: 'división cruzada' },
    ];

    render(<TreeNodeList nodes={nodes} />);

    expect(screen.getByText('Torneo de prueba')).toBeInTheDocument();
    expect(screen.getByText('Zona A')).toBeInTheDocument();
    expect(screen.getByText('Fase de grupos')).toBeInTheDocument();
    expect(screen.getByText('Copa Club12')).toBeInTheDocument();
    expect(screen.getByText('división cruzada')).toBeInTheDocument();
  });

  it('renders nothing but stays mounted for an empty node list', () => {
    const { container } = render(<TreeNodeList nodes={[]} />);
    expect(container.querySelector('div')).toBeInTheDocument();
  });
});
