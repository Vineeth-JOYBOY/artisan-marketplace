import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const { resetPassword } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    try {
      await resetPassword(email, token, password);
      setDone(true);
    } catch {
      setError('That reset link is invalid or has expired.');
    }
  }

  if (!email || !token) {
    return (
      <div>
        <h2>Reset password</h2>
        <p className="error">This reset link is missing or invalid.</p>
        <p><Link to="/forgot-password">Request a new link</Link></p>
      </div>
    );
  }

  if (done) {
    return (
      <div>
        <h2>Password reset</h2>
        <p>Your password has been updated.</p>
        <button className="btn" onClick={() => navigate('/login')}>Log in</button>
      </div>
    );
  }

  return (
    <div>
      <h2>Reset password</h2>
      <form onSubmit={handleSubmit}>
        <input type="password" placeholder="New password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
        <input type="password" placeholder="Confirm new password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} required minLength={6} />
        {error && <p className="error">{error}</p>}
        <button className="btn" type="submit">Reset password</button>
      </form>
    </div>
  );
}
