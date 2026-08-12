import * as Haptics from 'expo-haptics';

function safely(trigger: () => Promise<void>) {
  try {
    void trigger().catch(() => undefined);
  } catch {
    // Haptics are optional (for example, web and unsupported devices). Feedback
    // must never affect the underlying user action.
  }
}

export const feedback = {
  success: () => safely(() => Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success)),
  warning: () => safely(() => Haptics.notificationAsync(Haptics.NotificationFeedbackType.Warning)),
  selection: () => safely(() => Haptics.selectionAsync()),
};
