import Ionicons from '@expo/vector-icons/Ionicons';
import type { ComponentProps } from 'react';

type IconName = ComponentProps<typeof Ionicons>['name'];

type TabBarIconProps = {
  active: IconName;
  inactive: IconName;
  focused: boolean;
  color: string;
  size: number;
};

export function TabBarIcon({ active, inactive, focused, color, size }: TabBarIconProps) {
  return <Ionicons name={focused ? active : inactive} color={color} size={size} />;
}
