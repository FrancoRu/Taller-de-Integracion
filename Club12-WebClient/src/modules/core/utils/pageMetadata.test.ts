import { afterEach, describe, expect, it } from 'vitest';
import {
  DEFAULT_PAGE_METADATA,
  resetPageMetadata,
  setPageMetadata,
  toAbsoluteUrl,
} from '@/modules/core/utils/pageMetadata';

const metaContent = (attribute: 'property' | 'name', key: string) =>
  document.head
    .querySelector<HTMLMetaElement>(`meta[${attribute}="${key}"]`)
    ?.getAttribute('content');

const canonicalHref = () =>
  document.head
    .querySelector<HTMLLinkElement>('link[rel="canonical"]')
    ?.getAttribute('href');

describe('toAbsoluteUrl', () => {
  it('returns undefined for an empty path', () => {
    expect(toAbsoluteUrl(undefined, 'https://club12.com')).toBeUndefined();
    expect(toAbsoluteUrl('', 'https://club12.com')).toBeUndefined();
  });

  it('leaves an already-absolute URL untouched', () => {
    expect(toAbsoluteUrl('https://cdn/x.png', 'https://club12.com')).toBe(
      'https://cdn/x.png'
    );
    expect(toAbsoluteUrl('http://cdn/x.png', 'https://club12.com')).toBe(
      'http://cdn/x.png'
    );
  });

  it('joins a root-relative path onto the origin', () => {
    expect(toAbsoluteUrl('/assets/logo.png', 'https://club12.com')).toBe(
      'https://club12.com/assets/logo.png'
    );
  });

  it('joins a bare path onto the origin with a separator', () => {
    expect(toAbsoluteUrl('assets/logo.png', 'https://club12.com')).toBe(
      'https://club12.com/assets/logo.png'
    );
  });

  it('returns the relative path unchanged when there is no origin', () => {
    expect(toAbsoluteUrl('/assets/logo.png', '')).toBe('/assets/logo.png');
  });
});

describe('pageMetadata', () => {
  afterEach(() => {
    document.head.querySelectorAll('meta').forEach(meta => meta.remove());
    document.head
      .querySelectorAll('link[rel="canonical"]')
      .forEach(link => link.remove());
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

  it('writes a canonical link defaulting to the current origin + path', () => {
    setPageMetadata({ title: 'Campeones' });

    const canonical = canonicalHref();
    expect(canonical).toBeDefined();
    expect(canonical).toBe(
      `${window.location.origin}${window.location.pathname}`
    );
  });

  it('honours an explicit canonical url', () => {
    setPageMetadata({ title: 'Post', url: 'https://club12.com/blog/x' });

    expect(canonicalHref()).toBe('https://club12.com/blog/x');
  });
});
