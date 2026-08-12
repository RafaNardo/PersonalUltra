export const colors = {
  background: '#020202',
  surface: '#16161A',
  surfaceElevated: '#202027',
  primary: '#DF001A',
  primaryPressed: '#B80016',
  textPrimary: '#FAFAFA',
  textSecondary: '#B6B6C2',
  textMuted: '#74747F',
  success: '#4FD18B',
  warning: '#F0B04C',
  danger: '#F06272',
  border: '#303039',
} as const;

export const spacing = { xxs: 4, xs: 8, sm: 12, md: 16, lg: 20, xl: 24, xxl: 32, xxxl: 40, huge: 48 } as const;

export const typography = {
  displayXL: { fontFamily: 'MontserratExtraBold', fontSize: 40, lineHeight: 46, fontWeight: '400' as const },
  displayLG: { fontFamily: 'MontserratExtraBold', fontSize: 32, lineHeight: 38, fontWeight: '400' as const },
  headingLG: { fontFamily: 'MontserratBold', fontSize: 24, lineHeight: 30, fontWeight: '400' as const },
  headingMD: { fontFamily: 'MontserratBold', fontSize: 18, lineHeight: 24, fontWeight: '400' as const },
  bodyLG: { fontFamily: 'MontserratMedium', fontSize: 16, lineHeight: 23, fontWeight: '400' as const },
  bodyMD: { fontFamily: 'MontserratRegular', fontSize: 14, lineHeight: 20, fontWeight: '400' as const },
  caption: { fontFamily: 'MontserratSemiBold', fontSize: 12, lineHeight: 16, fontWeight: '400' as const },
  metricXL: { fontFamily: 'MontserratExtraBold', fontSize: 36, lineHeight: 40, fontWeight: '400' as const },
} as const;

export const radius = { sm: 10, md: 16, lg: 22, pill: 999 } as const;
