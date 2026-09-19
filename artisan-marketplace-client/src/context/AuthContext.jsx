import { createContext, useContext, useEffect, useState } from 'react';
import client from '../api/client';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('artisan_user');
    return stored ? JSON.parse(stored) : null;
  });

  useEffect(() => {
    if (user) {
      localStorage.setItem('artisan_user', JSON.stringify(user));
    } else {
      localStorage.removeItem('artisan_user');
    }
  }, [user]);

  async function register(email, password, fullName) {
    const { data } = await client.post('/auth/register', { email, password, fullName });
    localStorage.setItem('artisan_token', data.token);
    setUser(data.user);
    return data.user;
  }

  async function login(email, password) {
    const { data } = await client.post('/auth/login', { email, password });
    localStorage.setItem('artisan_token', data.token);
    setUser(data.user);
    return data.user;
  }

  function logout() {
    localStorage.removeItem('artisan_token');
    setUser(null);
  }

  async function forgotPassword(email) {
    await client.post('/auth/forgot-password', { email });
  }

  async function resetPassword(email, token, newPassword) {
    await client.post('/auth/reset-password', { email, token, newPassword });
  }

  return (
    <AuthContext.Provider value={{ user, register, login, logout, forgotPassword, resetPassword }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
