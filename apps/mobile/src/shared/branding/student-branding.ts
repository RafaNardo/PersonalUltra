import { colors } from '@/src/design/tokens';

/**
 * Resolves the Student accent without allowing trainer branding to replace
 * semantic feedback colors. M1 will supply `primaryColor` from the Student API.
 */
export function studentBranding(primaryColor?: string | null) {
  const primary = isHexColor(primaryColor) ? primaryColor : colors.primary;

  return {
    primary,
    primaryPressed: darkenHex(primary, 0.16),
    success: colors.success,
    warning: colors.warning,
    danger: colors.danger,
  } as const;
}

function isHexColor(value: string | null | undefined): value is string {
  return typeof value === 'string' && /^#[0-9A-Fa-f]{6}$/.test(value);
}

function darkenHex(hex: string, amount: number) {
  const channels = [1, 3, 5].map((index) => Math.round(parseInt(hex.slice(index, index + 2), 16) * (1 - amount)));
  return `#${channels.map((channel) => channel.toString(16).padStart(2, '0')).join('')}`;
}
