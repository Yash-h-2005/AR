import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';

export default function AdminLogin() {
  const [email, setEmail] = useState('admin@surakshaar.org');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleLogin = (e) => {
    e.preventDefault();
    if (!password) {
      setError('Password is required!');
      return;
    }
    // Simulate Admin Auth
    navigate('/dashboard');
  };

  return (
    <div>
      <nav className="navbar">
        <Link to="/" className="brand-logo">SurakshaAR</Link>
        <div className="nav-links">
          <Link to="/verify">Verify Certificate</Link>
        </div>
      </nav>

      <div className="container" style={{ maxWidth: '450px' }}>
        <div className="card">
          <h2 style={{ textAlign: 'center', marginBottom: '0.5rem', color: 'var(--orange-primary)' }}>Compliance Admin Login</h2>
          <p style={{ textAlign: 'center', color: 'var(--text-muted)', marginBottom: '1.5rem' }}>
            Authorized Safety Trainers & Plant Managers Portal
          </p>

          {error && (
            <div style={{ padding: '0.75rem', background: '#FEE2E2', color: '#991B1B', borderRadius: '8px', marginBottom: '1rem', fontSize: '0.9rem' }}>
              {error}
            </div>
          )}

          <form onSubmit={handleLogin}>
            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 700, marginBottom: '0.25rem' }}>Admin Email</label>
            <input
              type="email"
              className="input-field"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />

            <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 700, marginBottom: '0.25rem' }}>Password</label>
            <input
              type="password"
              className="input-field"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />

            <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '0.5rem' }}>
              LOGIN TO DASHBOARD →
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
