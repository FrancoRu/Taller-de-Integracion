/**
 * `@media print` isolation: hide every element on the page except the
 * subtree tagged `[data-print="sheet"]`, and force that subtree visible even
 * though it is `display:none` on screen. This hides all app chrome
 * (nav/tabs/buttons) without needing to tag them individually. Shared by
 * every print-only sheet (standings, goleadores, …) so they all get the
 * exact same isolation behavior from one place. Import and render via
 * `<GlobalStyles styles={printMediaStyles} />` in each sheet component —
 * only the currently-mounted sheet's `[data-print="sheet"]` node matters, so
 * it is safe for more than one sheet component to inject this at once.
 */
export const printMediaStyles = {
  '@media print': {
    'body *': { visibility: 'hidden' },
    '[data-print="sheet"], [data-print="sheet"] *': { visibility: 'visible' },
    '[data-print="sheet"]': {
      display: 'block !important',
      position: 'absolute',
      top: 0,
      left: 0,
      width: '100%',
    },
    '[data-print="hide"]': { display: 'none !important' },
    thead: { display: 'table-header-group' },
    tr: { breakInside: 'avoid' },
    '*': {
      printColorAdjust: 'exact',
      WebkitPrintColorAdjust: 'exact',
    },
  },
};
