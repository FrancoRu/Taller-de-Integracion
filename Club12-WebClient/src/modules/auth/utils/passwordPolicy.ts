/**
 * Client-side mirror of the backend password policy (Identity defaults).
 * Kept in one place so every password-setting screen (activation, reset,
 * change password) validates the same rules and shows the same wording.
 */

export interface PasswordPolicyState {
  requiredLength: boolean;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireNonAlphanumeric: boolean;
  requiredUniqueChars: boolean;
}

export const getPasswordPolicyState = (password: string): PasswordPolicyState => {
  const uniqueCharsCount = new Set(password).size;

  return {
    requiredLength: password.length >= 8,
    requireUppercase: /[A-Z]/.test(password),
    requireLowercase: /[a-z]/.test(password),
    requireDigit: /\d/.test(password),
    requireNonAlphanumeric: /[^a-zA-Z0-9]/.test(password),
    requiredUniqueChars: uniqueCharsCount >= 2,
  };
};

/**
 * Builds the list of human-readable validation messages for a new password and
 * its confirmation. Returns an empty array when everything is valid.
 */
export const buildPasswordPolicyMessages = (
  newPassword: string,
  confirmPassword: string
): string[] => {
  const policy = getPasswordPolicyState(newPassword);
  const messages: string[] = [];

  if (!newPassword) {
    messages.push('La nueva contraseña es obligatoria.');
  }

  if (!policy.requiredLength) {
    messages.push('La contraseña debe tener al menos 8 caracteres.');
  }

  if (!policy.requireUppercase) {
    messages.push('La contraseña debe contener al menos una letra mayúscula.');
  }

  if (!policy.requireLowercase) {
    messages.push('La contraseña debe contener al menos una letra minúscula.');
  }

  if (!policy.requireDigit) {
    messages.push('La contraseña debe contener al menos un número.');
  }

  if (!policy.requireNonAlphanumeric) {
    messages.push(
      'La contraseña debe contener al menos un carácter no alfanumérico.'
    );
  }

  if (!policy.requiredUniqueChars) {
    messages.push('La contraseña debe contener al menos 2 caracteres únicos.');
  }

  if (!confirmPassword) {
    messages.push('La confirmación de contraseña es obligatoria.');
  }

  if (newPassword && confirmPassword && newPassword !== confirmPassword) {
    messages.push('La confirmación no coincide con la nueva contraseña.');
  }

  return messages;
};

/**
 * Ordered checklist rendered under the password field so the user sees which
 * rules they still need to satisfy.
 */
export const PASSWORD_POLICY_RULES: {
  key: keyof PasswordPolicyState;
  label: string;
}[] = [
  { key: 'requiredLength', label: 'Mínimo 8 caracteres' },
  { key: 'requireUppercase', label: 'Al menos una mayúscula' },
  { key: 'requireLowercase', label: 'Al menos una minúscula' },
  { key: 'requireDigit', label: 'Al menos un número' },
  { key: 'requireNonAlphanumeric', label: 'Al menos un carácter especial' },
  { key: 'requiredUniqueChars', label: 'Al menos 2 caracteres únicos' },
];
