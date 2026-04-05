export interface User {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface Game {
  id: string;
  pgn: string;
  white: string;
  black: string;
  result: string;
  event?: string;
  site?: string;
  date?: string;
  createdAt: string;
  analyses?: Analysis[];
}

export interface Analysis {
  id: string;
  moveNumber: number;
  side: 'w' | 'b';
  fen: string;
  move: string;
  explanation: ChernevExplanation;
}

export interface ChernevExplanation {
  moveQuality: string;
  whyItWasPlayed: string;
  alternatives: CandidateMove[];
  strategicIdeas: string[];
  whatToConsider: string;
  evaluation?: number;
  bestMoveEvaluation?: number;
}

export interface CandidateMove {
  move: string;
  reason: string;
  evaluation: number;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface CreateGameRequest {
  pgn: string;
  white: string;
  black: string;
  result: string;
  event?: string;
  site?: string;
  date?: string;
}

export interface MoveInfo {
  san: string;
  uci: string;
  fen: string;
  moveNumber: number;
  side: 'w' | 'b';
}