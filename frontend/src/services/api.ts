import axios from 'axios';
import type { 
  User, 
  Game, 
  AuthResponse, 
  RegisterRequest, 
  LoginRequest,
  CreateGameRequest,
  Analysis
} from '@/types/chess';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const authApi = {
  register: (data: RegisterRequest) => 
    api.post<AuthResponse>('/auth/register', data),
  
  login: (data: LoginRequest) => 
    api.post<AuthResponse>('/auth/login', data),
  
  me: () => 
    api.get<User>('/auth/me'),
  
  google: () => api.get('/auth/google'),
  github: () => api.get('/auth/github')
};

export const gamesApi = {
  getAll: () => 
    api.get<Game[]>('/games'),
  
  getById: (id: string) => 
    api.get<Game>(`/games/${id}`),
  
  create: (data: CreateGameRequest) => 
    api.post<Game>('/games', data),
  
  update: (id: string, data: Partial<CreateGameRequest>) => 
    api.put<Game>(`/games/${id}`, data),
  
  delete: (id: string) => 
    api.delete(`/games/${id}`),
  
  import: (pgn: string) => 
    api.post<Game>('/games/import', { pgn })
};

export const analysisApi = {
  getGameAnalysis: (gameId: string) => 
    api.get<Analysis[]>(`/analysis/${gameId}`),
  
  analyzeGame: (gameId: string) => 
    api.post<Analysis[]>(`/analysis/${gameId}/analyze`, {})
};

export default api;