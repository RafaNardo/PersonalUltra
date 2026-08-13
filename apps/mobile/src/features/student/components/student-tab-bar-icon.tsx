import Ionicons from '@expo/vector-icons/Ionicons';
import type { ComponentProps } from 'react';
import { StyleSheet, View } from 'react-native';
import { colors, radius } from '@/src/design/tokens';

type IconName = ComponentProps<typeof Ionicons>['name'];

type StudentTabBarIconProps = {
  active: IconName;
  inactive: IconName;
  focused: boolean;
  color: string;
};

export function StudentTabBarIcon({ active, inactive, focused, color }: StudentTabBarIconProps) {
  return (
    <View style={[styles.iconShell, focused && styles.iconShellFocused]}>
      <Ionicons name={focused ? active : inactive} color={color} size={focused ? 22 : 21} />
    </View>
  );
}

const styles = StyleSheet.create({
  iconShell: {
    width: 40,
    height: 28,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radius.pill,
  },
  iconShellFocused: {
    backgroundColor: '#3A1D0C',
  },
});
