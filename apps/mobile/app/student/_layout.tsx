import { Tabs } from 'expo-router';
import { Text } from 'react-native';
import { colors, typography } from '@/src/design/tokens';

function TabIcon({ symbol, focused }: { symbol: string; focused: boolean }) { return <Text style={{ color: focused ? colors.primary : colors.textMuted, fontSize: 18 }}>{symbol}</Text>; }

export default function AppLayout() {
  return <Tabs screenOptions={{ headerShown: false, tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border, height: 68, paddingTop: 7 }, tabBarLabelStyle: { ...typography.caption, fontSize: 10 }, tabBarActiveTintColor: colors.primary, tabBarInactiveTintColor: colors.textMuted }}>
    <Tabs.Screen name="home" options={{ title: 'Início', tabBarIcon: ({ focused }) => <TabIcon symbol="⌂" focused={focused} /> }} />
    <Tabs.Screen name="training" options={{ title: 'Treino', tabBarIcon: ({ focused }) => <TabIcon symbol="●" focused={focused} /> }} />
    <Tabs.Screen name="coach" options={{ title: 'Coach', tabBarIcon: ({ focused }) => <TabIcon symbol="✦" focused={focused} /> }} />
    <Tabs.Screen name="nutrition" options={{ title: 'Nutrição', tabBarIcon: ({ focused }) => <TabIcon symbol="◉" focused={focused} /> }} />
    <Tabs.Screen name="progress" options={{ title: 'Progresso', tabBarIcon: ({ focused }) => <TabIcon symbol="↗" focused={focused} /> }} />
    <Tabs.Screen name="index" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="access" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="pain" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="meal/[id]" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="workout/[id]" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="training-plan/[id]" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="exercise/[sessionId]/[exerciseId]" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="rest" options={{ href: null, tabBarStyle: { display: 'none' } }} />
    <Tabs.Screen name="summary/[id]" options={{ href: null, tabBarStyle: { display: 'none' } }} />
  </Tabs>;
}
