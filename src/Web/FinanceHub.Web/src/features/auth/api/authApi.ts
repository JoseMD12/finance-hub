import { httpClient } from '@/shared/api/httpClient';
import { API_ENDPOINTS } from '@/shared/api/apiEndpoints';

export interface DevTokenResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
}

export const requestDevTokenApi = async (userId: string = 'usr_dev_001'): Promise<DevTokenResponse> => {
  const response = await httpClient.post<DevTokenResponse>(
    API_ENDPOINTS.AUTH.DEV_TOKEN,
    { userId }
  );
  return response.data;
};
