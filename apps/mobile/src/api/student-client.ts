import { api, ApiError } from './client';

const demoToken = 'personal-ultra-demo-student';

export const studentClient = {
  ...api,
  demoIdentity: () => api.demoIdentity(demoToken),
};

export { ApiError };
