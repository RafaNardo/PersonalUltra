import { Tabs } from 'expo-router';
import { StudentTabBarIcon } from '@/src/features/student/components/student-tab-bar-icon';
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
          borderTopWidth: 1,
          height: 78,
          paddingBottom: 10,
          paddingTop: 7,
        },
        tabBarItemStyle: { paddingVertical: 2 },
        tabBarIconStyle: { marginBottom: 1 },
        tabBarLabelStyle: { ...typography.caption, fontSize: 10, lineHeight: 13 },
      }}
    >
      <Tabs.Screen name="index" options={{ title: 'Início', tabBarIcon: (props) => <StudentTabBarIcon {...props} active="home" inactive="home-outline" /> }} />
      <Tabs.Screen name="training" options={{ title: 'Treino', tabBarIcon: (props) => <StudentTabBarIcon {...props} active="barbell" inactive="barbell-outline" /> }} />
      <Tabs.Screen name="coach" options={{ title: 'Coach', tabBarIcon: (props) => <StudentTabBarIcon {...props} active="sparkles" inactive="sparkles-outline" /> }} />
      <Tabs.Screen name="nutrition" options={{ title: 'Nutrição', tabBarIcon: (props) => <StudentTabBarIcon {...props} active="restaurant" inactive="restaurant-outline" /> }} />
      <Tabs.Screen name="progress" options={{ title: 'Progresso', tabBarIcon: (props) => <StudentTabBarIcon {...props} active="trending-up" inactive="trending-up-outline" /> }} />
    </Tabs>
  );
}
