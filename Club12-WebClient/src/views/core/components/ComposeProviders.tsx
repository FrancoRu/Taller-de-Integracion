import { ComponentType, ReactNode } from 'react';

interface ComposeProvidersProps {
  providers: ComponentType<{ children: ReactNode }>[];
  children: ReactNode;
}

const ComposeProviders = ({ providers, children }: ComposeProvidersProps) =>
  providers.reduceRight<ReactNode>(
    (acc, Provider) => <Provider>{acc}</Provider>,
    children
  );

export default ComposeProviders;
