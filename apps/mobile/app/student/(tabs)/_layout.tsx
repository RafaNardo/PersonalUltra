import { Tabs } from 'expo-router';
import { TabBarIcon } from '@/src/components/tab-bar-icon';
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
      <Tabs.Screen name="index" options={{ title: 'Início', tabBarIcon: (props) => <TabBarIcon {...props} active="home" inactive="home-outline" /> }} />
      <Tabs.Screen name="training" options={{ title: 'Treino', tabBarIcon: (props) => <TabBarIcon {...props} active="barbell" inactive="barbell-outline" /> }} />
      <Tabs.Screen name="coach" options={{ title: 'Coach', tabBarIcon: (props) => <TabBarIcon {...props} active="sparkles" inactive="sparkles-outline" /> }} />
      <Tabs.Screen name="nutrition" options={{ title: 'Nutrição', tabBarIcon: (props) => <TabBarIcon {...props} active="restaurant" inactive="restaurant-outline" /> }} />
      <Tabs.Screen name="progress" options={{ title: 'Progresso', tabBarIcon: (props) => <TabBarIcon {...props} active="trending-up" inactive="trending-up-outline" /> }} />
    </Tabs>
  );
}
