import { Tabs } from 'expo-router';
import { Text } from 'react-native';
import { colors, typography } from '@/src/design/tokens';

function TabIcon({ symbol, focused }: { symbol: string; focused: boolean }) {
  return <Text style={{ color: focused ? colors.primary : colors.textMuted, fontSize: 18 }}>{symbol}</Text>;
}

export default function StudentTabsLayout() {
  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textMuted,
        tabBarStyle: {
          backgroundColor: colors.surface,
          borderTopColor: colors.border,
          height: 72,
          paddingBottom: 10,
          paddingTop: 8,
        },
        tabBarLabelStyle: { ...typography.caption, fontSize: 11 },
      }}
    >
      <Tabs.Screen name="index" options={{ title: 'Início', tabBarIcon: ({ focused }) => <TabIcon symbol="⌂" focused={focused} /> }} />
      <Tabs.Screen name="training" options={{ title: 'Treino', tabBarIcon: ({ focused }) => <TabIcon symbol="●" focused={focused} /> }} />
      <Tabs.Screen name="coach" options={{ title: 'Coach', tabBarIcon: ({ focused }) => <TabIcon symbol="✦" focused={focused} /> }} />
      <Tabs.Screen name="nutrition" options={{ title: 'Nutrição', tabBarIcon: ({ focused }) => <TabIcon symbol="◉" focused={focused} /> }} />
      <Tabs.Screen name="progress" options={{ title: 'Progresso', tabBarIcon: ({ focused }) => <TabIcon symbol="↗" focused={focused} /> }} />
    </Tabs>
  );
}
