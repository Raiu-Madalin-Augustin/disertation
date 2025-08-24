// src/lib/api.ts
import axios, { AxiosHeaders } from 'axios';
import { getUser, isAdmin } from '../store/auth';

const api = axios.create({
  baseURL: 'https://localhost:7286/',
  withCredentials: false,
});

api.interceptors.request.use((config) => {
  const u = getUser();
  if (u && isAdmin(u)) {
    if (!config.headers) {
      config.headers = new AxiosHeaders();
    }
    (config.headers as any)['X-User-Id'] = u.id.toString();
  }
  return config;
});

export default api;
