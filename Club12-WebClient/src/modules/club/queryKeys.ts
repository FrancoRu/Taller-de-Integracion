export const clubKeys = {
  history: (idOrSlug: string) => ['club', 'history', idOrSlug] as const,
  all: () => ['club', 'all'] as const,
};
