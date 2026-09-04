export const BLOG_EXCERPT_LENGTH = 150;
export const BLOG_HOME_EXCERPT_LENGTH = 160;

// Well under the Slug column's 220-char limit (Slug is derived from Title
// and can get a "-2" suffix for uniqueness), so a max-length title can never
// overflow it.
export const BLOG_TITLE_MAX_LENGTH = 150;
