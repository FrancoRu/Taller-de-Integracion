import { useEffect } from 'react';

/**
 * Per-page social/SEO metadata (HU-17). Because the app is a client-rendered
 * SPA (no SSR), these tags are written into the document head at runtime. That
 * covers on-page sharing widgets and crawlers that execute JavaScript; static
 * scrapers that never run JS will still read the index.html defaults.
 */
export interface PageMetadata {
  /** Document title / og:title / twitter:title. */
  title?: string;
  /** Meta description / og:description / twitter:description. */
  description?: string;
  /** Absolute image URL for og:image / twitter:image. */
  image?: string;
  /** Canonical URL for og:url. Defaults to the current location. */
  url?: string;
  /** og:type. Defaults to "website". */
  type?: string;
}

const SITE_NAME = 'Club 12';

/** The site-wide default metadata restored when a page unmounts (HU-17). */
export const DEFAULT_PAGE_METADATA: PageMetadata = {
  title: SITE_NAME,
  description:
    'La liga de básquet amateur con más historia de la zona. Torneos, ' +
    'resultados y estadísticas de todas las divisiones en un solo lugar.',
  type: 'website',
};

const upsertMeta = (
  attribute: 'property' | 'name',
  key: string,
  content?: string
): void => {
  if (typeof document === 'undefined') {
    return;
  }

  const selector = `meta[${attribute}="${key}"]`;
  let element = document.head.querySelector<HTMLMetaElement>(selector);

  if (!content) {
    element?.remove();
    return;
  }

  if (!element) {
    element = document.createElement('meta');
    element.setAttribute(attribute, key);
    document.head.appendChild(element);
  }

  element.setAttribute('content', content);
};

/**
 * Writes the given Open Graph / Twitter Card metadata into the document head,
 * merging over the site defaults (HU-17). Title and description always fall
 * back to the defaults; image and url are only emitted when provided.
 *
 * @param metadata The page-specific overrides.
 */
export const setPageMetadata = (metadata: PageMetadata): void => {
  if (typeof document === 'undefined') {
    return;
  }

  const title = metadata.title ?? DEFAULT_PAGE_METADATA.title;
  const description =
    metadata.description ?? DEFAULT_PAGE_METADATA.description;
  const type = metadata.type ?? DEFAULT_PAGE_METADATA.type;
  const url =
    metadata.url ??
    (typeof window !== 'undefined' ? window.location.href : undefined);
  const { image } = metadata;

  const documentTitle =
    title && title !== SITE_NAME ? `${title} · ${SITE_NAME}` : SITE_NAME;
  document.title = documentTitle;

  upsertMeta('name', 'description', description);

  upsertMeta('property', 'og:site_name', SITE_NAME);
  upsertMeta('property', 'og:title', title);
  upsertMeta('property', 'og:description', description);
  upsertMeta('property', 'og:type', type);
  upsertMeta('property', 'og:url', url);
  upsertMeta('property', 'og:image', image);

  upsertMeta('name', 'twitter:card', image ? 'summary_large_image' : 'summary');
  upsertMeta('name', 'twitter:title', title);
  upsertMeta('name', 'twitter:description', description);
  upsertMeta('name', 'twitter:image', image);
};

/** Restores the site-wide default metadata (HU-17). */
export const resetPageMetadata = (): void => {
  setPageMetadata(DEFAULT_PAGE_METADATA);
};

/**
 * React hook that applies per-page metadata on mount/update and restores the
 * site defaults on unmount (HU-17). Pass a stable/serialisable metadata object.
 *
 * @param metadata The page-specific metadata to apply.
 */
export const usePageMetadata = (metadata: PageMetadata): void => {
  const { title, description, image, url, type } = metadata;

  useEffect(() => {
    setPageMetadata({ title, description, image, url, type });

    return () => {
      resetPageMetadata();
    };
  }, [title, description, image, url, type]);
};
