import { TextInput, type TextInputProps } from 'react-native';

export function formatBrazilianPhone(value: string) {
  const digits = value.replace(/\D/g, '').slice(0, 11);
  if (digits.length <= 2) return digits ? `(${digits}` : '';
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
}

export function PhoneInput({ value, onChangeText, ...props }: Omit<TextInputProps, 'value' | 'onChangeText' | 'keyboardType'> & { value: string; onChangeText: (value: string) => void }) {
  return <TextInput {...props} value={value} onChangeText={(next) => onChangeText(formatBrazilianPhone(next))} keyboardType="phone-pad" />;
}
