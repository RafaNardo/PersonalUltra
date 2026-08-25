import { Tabs } from 'expo-router';
import { TabBarIcon } from '@/src/components/tab-bar-icon';
import { colors, typography } from '@/src/design/tokens';

export default function TrainerLayout() {
  return <Tabs screenOptions={{ headerShown: false, tabBarActiveTintColor: colors.primary, tabBarInactiveTintColor: colors.textMuted, tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border, height: 72, paddingBottom: 10, paddingTop: 8 }, tabBarLabelStyle: { ...typography.caption, fontSize: 11 } }}>
    <Tabs.Screen name="index" options={{ title: 'Início', tabBarIcon: (props) => <TabBarIcon {...props} active="home" inactive="home-outline" /> }} />
    <Tabs.Screen name="students" options={{ title: 'Alunos', tabBarIcon: (props) => <TabBarIcon {...props} active="people" inactive="people-outline" /> }} />
    <Tabs.Screen name="training" options={{ title: 'Treinos', tabBarIcon: (props) => <TabBarIcon {...props} active="barbell" inactive="barbell-outline" /> }} />
    <Tabs.Screen name="nutrition" options={{ title: 'Alimentação', tabBarIcon: (props) => <TabBarIcon {...props} active="restaurant" inactive="restaurant-outline" /> }} />
    <Tabs.Screen name="settings" options={{ title: 'Configurações', tabBarIcon: (props) => <TabBarIcon {...props} active="settings" inactive="settings-outline" /> }} />
    <Tabs.Screen name="invite" options={{ href: null }} />
    <Tabs.Screen name="students/[id]" options={{ href: null }} />
    <Tabs.Screen name="students/nutrition/[id]" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/nutrition/add" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/nutrition/from-template" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/[workoutId]" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/add" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/from-template" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/new" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/[workoutId]/catalog/index" options={{ href: null }} />
    <Tabs.Screen name="students/[studentId]/workouts/[workoutId]/catalog/[exerciseId]" options={{ href: null }} />
    <Tabs.Screen name="training/templates" options={{ href: null }} />
    <Tabs.Screen name="training/templates/[id]" options={{ href: null }} />
    <Tabs.Screen name="nutrition/templates" options={{ href: null }} />
    <Tabs.Screen name="nutrition/templates/[id]" options={{ href: null }} />
    <Tabs.Screen name="training/[id]" options={{ href: null }} />
  </Tabs>;
}
