import Swal from 'sweetalert2';
import { CANCEL_BUTTON_COLOR, getTheme } from '@/theme';

const theme = getTheme('dark');
const DIALOG_BACKGROUND = theme.palette.background.paper;
const DIALOG_TEXT_COLOR = theme.palette.text.primary;

// SweetAlert's container defaults to z-index 1060, which sits BELOW MUI's
// modals/dialogs (1300+). A confirm/notify fired while a MUI Dialog is open
// then renders behind it — its buttons unclickable and the awaited promise
// never resolving. Lifting the container above every MUI layer keeps alerts
// on top wherever they are triggered from.
const OVER_MUI_ZINDEX = '2000';

const liftAboveMuiModals = () => {
  const container = Swal.getContainer();
  if (container) {
    container.style.zIndex = OVER_MUI_ZINDEX;
  }
};

type NotifyIcon = 'success' | 'error' | 'warning' | 'info';

interface NotifyOptions {
  title: string;
  text?: string;
}

interface ConfirmActionOptions {
  title: string;
  text?: string;
  icon?: 'warning' | 'question';
  confirmButtonText?: string;
  cancelButtonText?: string;
  confirmButtonColor?: string;
  cancelButtonColor?: string;
}

interface ConfirmDeleteOptions {
  title: string;
  text: string;
  confirmButtonText?: string;
}

async function notify(icon: NotifyIcon, options: NotifyOptions): Promise<void> {
  await Swal.fire({
    title: options.title,
    text: options.text,
    icon,
    confirmButtonColor: theme.palette.primary.main,
    background: DIALOG_BACKGROUND,
    color: DIALOG_TEXT_COLOR,
    didOpen: liftAboveMuiModals,
  });
}

/**
 * Shows a standardized success dialog using the app theme colors.
 * @param options - Title and optional text.
 */
export const notifySuccess = (options: NotifyOptions): Promise<void> =>
  notify('success', options);

/**
 * Shows a standardized error dialog using the app theme colors.
 * @param options - Title and optional text.
 */
export const notifyError = (options: NotifyOptions): Promise<void> =>
  notify('error', options);

/**
 * Shows a standardized warning dialog using the app theme colors.
 * Intended for validation messages that do not require confirmation.
 * @param options - Title and optional text.
 */
export const notifyWarning = (options: NotifyOptions): Promise<void> =>
  notify('warning', options);

/**
 * Shows a standardized info dialog using the app theme colors.
 * @param options - Title and optional text.
 */
export const notifyInfo = (options: NotifyOptions): Promise<void> =>
  notify('info', options);

/**
 * Shows a standardized confirm/cancel dialog. Button colors default to the
 * app theme but can be overridden for semantic actions (e.g. a destructive
 * action in red, an activation action in green).
 * @param options - Title, text, icon and optional button text/colors.
 * @returns `true` if the user confirmed the action, `false` otherwise.
 */
export async function confirmAction(options: ConfirmActionOptions): Promise<boolean> {
  const result = await Swal.fire({
    title: options.title,
    text: options.text,
    icon: options.icon ?? 'warning',
    showCancelButton: true,
    confirmButtonColor: options.confirmButtonColor ?? theme.palette.primary.main,
    cancelButtonColor: options.cancelButtonColor ?? CANCEL_BUTTON_COLOR,
    confirmButtonText: options.confirmButtonText ?? 'Confirmar',
    cancelButtonText: options.cancelButtonText ?? 'Cancelar',
    background: DIALOG_BACKGROUND,
    color: DIALOG_TEXT_COLOR,
    didOpen: liftAboveMuiModals,
  });

  return result.isConfirmed;
}

/**
 * Shows a standardized delete-confirmation dialog using the app theme colors.
 * @param options - Title, text and optional confirm button text.
 * @returns `true` if the user confirmed the action, `false` otherwise.
 */
export const confirmDelete = (options: ConfirmDeleteOptions): Promise<boolean> =>
  confirmAction({ ...options, confirmButtonText: options.confirmButtonText ?? 'Sí, eliminar' });
