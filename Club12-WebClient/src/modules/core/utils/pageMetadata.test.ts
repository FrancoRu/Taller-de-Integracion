import { afterEach, describe, expect, it } from 'vitest';
import {
  DEFAULT_PAGE_METADATA,
  resetPageMetadata,
  setPageMetadata,
} from '@/modules/core/utils/pageMetadata';

const metaContent = (attribute: 'property' | 'name', key: string) =>
  document.head
    .querySelector<HTMLMetaElement>(`meta[${attribute}="${key}"]`)
    ?.getAttribute('content');

describe('pageMetadata', () => {
  afterEach(() => {
    document.head.querySelectorAll('meta').forEach(meta => meta.remove());
    document.title = '';
  });

  it('writes Open Graph and Twitter tags for a post (HU-17)', () => {
    setPageMetadata({
      title: 'Gran final',
      description: 'Resumen del partido',
      image: 'https://cdn.club12/photo.png',
      url: 'https://club12/blog/gran-final',
      type: 'article',
    });

    expect(document.title).toBe('Gran final · Club 12');
    expect(metaContent('property', 'og:title')).toBe('Gran final');
    expect(metaContent('property', 'og:description')).toBe(
      'Resumen del partido'
    );
    expect(metaContent('property', 'og:image')).toBe(
      'https://cdn.club12/photo.png'
    );
    expect(metaContent('property', 'og:url')).toBe(
      'https://club12/blog/gran-final'
    );
    expect(metaContent('property', 'og:type')).toBe('article');
    expect(metaContent('name', 'twitter:card')).toBe('summary_large_image');
    expect(metaContent('name', 'twitter:title')).toBe('Gran final');
    expect(metaContent('name', 'twitter:image')).toBe(
      'https://cdn.club12/photo.png'
    );
  });

  it('falls back to a plain summary card when there is no image', () => {
    setPageMetadata({ title: 'Sin imagen', description: 'Texto' });

    expect(metaContent('name', 'twitter:card')).toBe('summary');
    expect(metaContent('property', 'og:image')).toBeUndefined();
    expect(metaContent('name', 'twitter:image')).toBeUndefined();
  });

  it('reset restores the site defaults', () => {
    setPageMetadata({ title: 'Gran final', description: 'x' });
    resetPageMetadata();

    expect(document.title).toBe('Club 12');
    expect(metaContent('property', 'og:title')).toBe(
      DEFAULT_PAGE_METADATA.title
    );
    expect(metaContent('property', 'og:description')).toBe(
      DEFAULT_PAGE_METADATA.description
    );
  });
});
