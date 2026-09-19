import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');
  const { forgotPassword } = useAuth();

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    try {
      await forgotPassword(email);
      setSubmitted(true);
    } catch {
      setError('Something went wrong. Please try again.');
    }
  }

  if (submitted) {
    return (
      <div>
        <h2>Check your email</h2>
        <p>If that email is registered, a password reset link has been sent.</p>
        <p><Link to="/login">Back to log in</Link></p>
      </div>
    );
  }

  return (
    <div>
      <h2>Forgot password</h2>
      <form onSubmit={handleSubmit}>
        <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        {error && <p className="error">{error}</p>}
        <button className="btn" type="submit">Send reset link</button>
      </form>
      <p><Link to="/login">Back to log in</Link></p>
    </div>
  );
}
