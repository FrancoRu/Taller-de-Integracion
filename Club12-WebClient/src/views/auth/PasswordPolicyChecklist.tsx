import { Divider, Stack, Typography } from '@mui/material';
import {
  PASSWORD_POLICY_RULES,
  getPasswordPolicyState,
} from '@/modules/auth/utils/passwordPolicy';

/**
 * Live checklist of the password rules (HU-09/HU-10). Each rule turns green
 * once the typed password satisfies it. Shared by the activation and
 * password-reset screens so both show identical wording and behaviour.
 */
export default function PasswordPolicyChecklist({
  password,
}: {
  password: string;
}) {
  const policy = getPasswordPolicyState(password);

  return (
    <Stack spacing={0.5}>
      <Typography variant="subtitle2">Reglas de contraseña</Typography>
      <Divider />
      {PASSWORD_POLICY_RULES.map(rule => (
        <Typography
          key={rule.key}
          variant="body2"
          color={policy[rule.key] ? 'success.main' : 'text.secondary'}
        >
          {policy[rule.key] ? '✓' : '•'} {rule.label}
        </Typography>
      ))}
    </Stack>
  );
}
