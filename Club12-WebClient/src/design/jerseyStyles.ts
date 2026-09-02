/**
 * Jersey templates — the selectable "kit" patterns a team can pick, in the
 * spirit of the Wikipedia team-kit diagrams (a fillable body plus a pattern
 * layer). Each template is rendered by {@link JerseySvg} from the team's two
 * colors: a primary body color and a secondary color used for the pattern and
 * trim. Authoring the shapes ourselves keeps them inline, tintable at runtime,
 * and free of any external asset.
 */
export type JerseyStyle =
  | 'solid'
  | 'stripes'
  | 'hoops'
  | 'diagonal'
  | 'chevron'
  | 'sash'
  | 'sides'
  | 'halves'
  | 'circles'
  | 'gradient'
  | 'vneck'
  | 'pinstripe'
  | 'yoke'
  | 'colorblock'
  | 'arrow';

export interface JerseyStyleOption {
  value: JerseyStyle;
  /** Spanish label shown in the picker. */
  label: string;
}

export const JERSEY_STYLES: JerseyStyleOption[] = [
  { value: 'solid', label: 'Lisa' },
  { value: 'stripes', label: 'Rayas verticales' },
  { value: 'hoops', label: 'Franjas horizontales' },
  { value: 'diagonal', label: 'Rayas diagonales' },
  { value: 'chevron', label: 'Chevrón' },
  { value: 'sash', label: 'Banda diagonal' },
  { value: 'sides', label: 'Laterales' },
  { value: 'halves', label: 'Mitades' },
  { value: 'circles', label: 'Lunares' },
  { value: 'gradient', label: 'Degradé' },
  { value: 'vneck', label: 'Cuello en V' },
  { value: 'pinstripe', label: 'Rayas finas' },
  { value: 'yoke', label: 'Canesú' },
  { value: 'colorblock', label: 'Bloque diagonal' },
  { value: 'arrow', label: 'Flecha' },
];

export const DEFAULT_JERSEY_STYLE: JerseyStyle = 'solid';

/** Narrows an arbitrary stored string to a known {@link JerseyStyle}. */
export const isJerseyStyle = (value: unknown): value is JerseyStyle =>
  typeof value === 'string' && JERSEY_STYLES.some(style => style.value === value);

/** Coerces a stored value into a valid style, defaulting when unrecognized. */
export const toJerseyStyle = (value: unknown): JerseyStyle =>
  isJerseyStyle(value) ? value : DEFAULT_JERSEY_STYLE;
