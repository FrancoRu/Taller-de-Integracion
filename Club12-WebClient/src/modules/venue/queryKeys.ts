export const venueKeys = {
  list: () => ['venue', 'list'] as const,
  byId: (idOrSlug: string) => ['venue', 'byId', idOrSlug] as const,
};
