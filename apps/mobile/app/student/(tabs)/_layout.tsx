import { Tabs } from 'expo-router';
import { colors, typography } from '@/src/design/tokens';

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
      <Tabs.Screen name="index" options={{ title: 'Início' }} />
      <Tabs.Screen name="training" options={{ title: 'Treino' }} />
      <Tabs.Screen name="coach" options={{ title: 'Coach' }} />
      <Tabs.Screen name="nutrition" options={{ title: 'Nutrição' }} />
      <Tabs.Screen name="progress" options={{ title: 'Progresso' }} />
    </Tabs>
  );
}
