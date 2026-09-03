export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  isAdmin: boolean;
}

export interface AuthResponse {
  token: string;
}
