import React, { useState } from 'react';
import { Link } from 'react-router-dom';

const initialWorkers = [
  { workerId: 'WRK001', name: 'Ramesh Kumar', module: '🔥 Fire Response', score: 88.0, certified: true, date: '2026-09-05', certId: 'SAR-2026-A1B2C3', status: 'VALID' },
  { workerId: 'WRK002', name: 'Anita Soren', module: '☣ Gas & Confined Space', score: 92.5, certified: true, date: '2026-09-04', certId: 'SAR-2026-X9Y8Z7', status: 'VALID' },
  { workerId: 'WRK003', name: 'Suresh Hansda', module: '🔥 Fire Response', score: 62.0, certified: false, date: '2026-09-03', certId: 'N/A', status: 'FAILED' },
  { workerId: 'WRK004', name: 'Pooja Mahato', module: '☣ Gas & Confined Space', score: 85.0, certified: true, date: '2026-09-02', certId: 'SAR-2026-P4Q5R6', status: 'VALID' },
  { workerId: 'WRK005', name: 'Vikram Singh', module: '🔥 Fire Response', score: 90.0, certified: true, date: '2026-09-01', certId: 'SAR-2026-V7W8X9', status: 'VALID' }
];

export default function AdminDashboard() {
  const [workers, setWorkers] = useState(initialWorkers);
  const [filterCategory, setFilterCategory] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');

  const handleRevoke = (certId) => {
    if (window.confirm(`Are you sure you want to REVOKE certificate ${certId}?`)) {
      setWorkers(workers.map(w => w.certId === certId ? { ...w, status: 'REVOKED', certified: false } : w));
    }
  };

  const filteredWorkers = workers.filter(w => {
    const matchesSearch = w.name.toLowerCase().includes(searchQuery.toLowerCase()) || w.workerId.toLowerCase().includes(searchQuery.toLowerCase());
    if (!matchesSearch) return false;
    if (filterCategory === 'CERTIFIED') return w.certified;
    if (filterCategory === 'UNCERTIFIED') return !w.certified;
    if (filterCategory === 'FIRE') return w.module.includes('Fire');
    if (filterCategory === 'GAS') return w.module.includes('Gas');
    return true;
  });

  return (
    <div>
      <nav className="navbar">
        <Link to="/" className="brand-logo">SurakshaAR — Compliance Admin</Link>
        <div className="nav-links">
          <Link to="/verify">Public Verification</Link>
          <Link to="/login" style={{ color: 'var(--red-danger)' }}>Logout</Link>
        </div>
      </nav>

      <div className="container">
        {/* Metric Summary Cards */}
        <div className="grid-metrics">
          <div className="metric-box">
            <div className="metric-val">45</div>
            <div className="metric-lbl">Total Registered Workers</div>
          </div>
          <div className="metric-box">
            <div className="metric-val" style={{ color: 'var(--green-success)' }}>40</div>
            <div className="metric-lbl">Trained Workers</div>
          </div>
          <div className="metric-box">
            <div className="metric-val" style={{ color: 'var(--green-success)' }}>35</div>
            <div className="metric-lbl">Certified Workers</div>
          </div>
          <div className="metric-box">
            <div className="metric-val" style={{ color: 'var(--amber-warning)' }}>5</div>
            <div className="metric-lbl">Pending Training</div>
          </div>
          <div className="metric-box">
            <div className="metric-val">88.5%</div>
            <div className="metric-lbl">Average Score</div>
          </div>
        </div>

        {/* Skill Risk Analytics Card */}
        <div className="card">
          <h3 style={{ marginBottom: '1rem', color: 'var(--slate-secondary)' }}>Skill & Failure Analytics (Calculated Telemetry)</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
            <div style={{ background: '#F8FAFC', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid var(--orange-primary)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Extinguisher Selection Error</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>34%</div>
            </div>
            <div style={{ background: '#F8FAFC', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid var(--amber-warning)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Exit Route Selection Error</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>21%</div>
            </div>
            <div style={{ background: '#F8FAFC', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid var(--orange-primary)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>PPE Respirator Selection Error</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>18%</div>
            </div>
            <div style={{ background: '#F8FAFC', padding: '1rem', borderRadius: '8px', borderLeft: '4px solid var(--red-danger)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Sequence Order Violations</div>
              <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>15%</div>
            </div>
          </div>
        </div>

        {/* Worker Compliance Table */}
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '1rem' }}>
            <h3>Worker Compliance Records</h3>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input
                type="text"
                className="input-field"
                placeholder="Search Worker ID or Name..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                style={{ width: '220px', marginBottom: 0 }}
              />
              <select
                className="input-field"
                value={filterCategory}
                onChange={(e) => setFilterCategory(e.target.value)}
                style={{ width: '160px', marginBottom: 0 }}
              >
                <option value="ALL">All Records</option>
                <option value="CERTIFIED">Certified Only</option>
                <option value="UNCERTIFIED">Uncertified Only</option>
                <option value="FIRE">Fire Module</option>
                <option value="GAS">Gas Module</option>
              </select>
            </div>
          </div>

          <table>
            <thead>
              <tr>
                <th>Worker ID</th>
                <th>Worker Name</th>
                <th>Training Module</th>
                <th>Score</th>
                <th>Status</th>
                <th>Training Date</th>
                <th>Certificate ID</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredWorkers.map(w => (
                <tr key={w.workerId}>
                  <td><strong>{w.workerId}</strong></td>
                  <td>{w.name}</td>
                  <td>{w.module}</td>
                  <td><strong>{w.score}%</strong></td>
                  <td>
                    {w.status === 'VALID' && <span className="badge badge-success">✓ PASSED</span>}
                    {w.status === 'REVOKED' && <span className="badge badge-danger">⚠ REVOKED</span>}
                    {w.status === 'FAILED' && <span className="badge badge-warning">✖ FAILED</span>}
                  </td>
                  <td>{w.date}</td>
                  <td><code>{w.certId}</code></td>
                  <td>
                    {w.status === 'VALID' ? (
                      <button className="btn btn-danger" style={{ padding: '0.3rem 0.6rem', fontSize: '0.8rem' }} onClick={() => handleRevoke(w.certId)}>
                        Revoke
                      </button>
                    ) : (
                      <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>N/A</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
