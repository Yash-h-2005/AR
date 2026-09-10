import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';

const mockCertificates = {
  'SAR-2026-A1B2C3': {
    certificateId: 'SAR-2026-A1B2C3',
    workerId: 'WRK001',
    workerName: 'Ramesh Kumar',
    moduleName: '🔥 Fire & Explosion Response',
    score: 88.0,
    issueDate: '2026-09-05',
    status: 'VALID'
  },
  'SAR-2026-X9Y8Z7': {
    certificateId: 'SAR-2026-X9Y8Z7',
    workerId: 'WRK002',
    workerName: 'Anita Soren',
    moduleName: '☣ Gas Leak & Confined Space',
    score: 92.5,
    issueDate: '2026-09-04',
    status: 'VALID'
  },
  'SAR-2026-REV123': {
    certificateId: 'SAR-2026-REV123',
    workerId: 'WRK003',
    workerName: 'Suresh Hansda',
    moduleName: '🔥 Fire & Explosion Response',
    score: 72.0,
    issueDate: '2026-08-15',
    status: 'REVOKED'
  }
};

export default function PublicVerification() {
  const { certId } = useParams();
  const [query, setQuery] = useState(certId || '');
  const [certificate, setCertificate] = useState(null);
  const [searched, setSearched] = useState(false);

  useEffect(() => {
    if (certId) {
      handleSearch(certId);
    }
  }, [certId]);

  const handleSearch = (idToSearch) => {
    const target = idToSearch || query;
    const cert = mockCertificates[target.trim()];
    setCertificate(cert || null);
    setSearched(true);
  };

  return (
    <div>
      <nav className="navbar">
        <Link to="/" className="brand-logo">SurakshaAR</Link>
        <div className="nav-links">
          <Link to="/verify">Verify Certificate</Link>
          <Link to="/login">Admin Login</Link>
        </div>
      </nav>

      <div className="container" style={{ maxWidth: '650px' }}>
        <div className="card" style={{ textAlign: 'center' }}>
          <h2 style={{ marginBottom: '0.5rem', color: 'var(--orange-primary)' }}>Certificate Verification Portal</h2>
          <p style={{ color: 'var(--text-muted)', marginBottom: '1.5rem' }}>
            Verify digital safety training credentials for industrial workers across Jharkhand mining & manufacturing sectors.
          </p>

          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <input
              type="text"
              className="input-field"
              placeholder="Enter Certificate ID (e.g. SAR-2026-A1B2C3)"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              style={{ marginBottom: 0 }}
            />
            <button className="btn btn-primary" onClick={() => handleSearch()}>Verify</button>
          </div>
        </div>

        {searched && (
          <div className="card" style={{ borderTop: `4px solid ${certificate ? (certificate.status === 'VALID' ? 'var(--green-success)' : 'var(--red-danger)') : 'var(--red-danger)'}` }}>
            {certificate ? (
              <div>
                <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
                  {certificate.status === 'VALID' ? (
                    <div>
                      <span className="badge badge-success" style={{ fontSize: '1.1rem', padding: '0.5rem 1rem' }}>✓ CERTIFICATE VERIFIED</span>
                      <h3 style={{ marginTop: '0.75rem', color: 'var(--green-success)' }}>Authentic Industrial Safety Credential</h3>
                    </div>
                  ) : (
                    <div>
                      <span className="badge badge-danger" style={{ fontSize: '1.1rem', padding: '0.5rem 1rem' }}>⚠ CERTIFICATE REVOKED</span>
                      <h3 style={{ marginTop: '0.75rem', color: 'var(--red-danger)' }}>This credential was revoked by Compliance Admin</h3>
                    </div>
                  )}
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', fontSize: '0.95rem' }}>
                  <div><strong>Worker Name:</strong> {certificate.workerName}</div>
                  <div><strong>Worker ID:</strong> {certificate.workerId}</div>
                  <div><strong>Certificate ID:</strong> {certificate.certificateId}</div>
                  <div><strong>Training Module:</strong> {certificate.moduleName}</div>
                  <div><strong>Assessment Score:</strong> {certificate.score}%</div>
                  <div><strong>Issue Date:</strong> {certificate.issueDate}</div>
                </div>

                <div style={{ marginTop: '1.5rem', paddingTop: '1rem', borderTop: '1px solid var(--border-color)', fontSize: '0.85rem', color: 'var(--text-muted)', textAlign: 'center' }}>
                  Verification Source: SurakshaAR Compliance Ledger (ISO/IEC 18004 Standard)
                </div>
              </div>
            ) : (
              <div style={{ textAlign: 'center', padding: '1rem' }}>
                <span className="badge badge-danger" style={{ fontSize: '1.1rem', padding: '0.5rem 1rem' }}>✖ CERTIFICATE NOT FOUND</span>
                <h3 style={{ marginTop: '0.75rem', color: 'var(--red-danger)' }}>No record found matching ID: {query}</h3>
                <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                  Please check the certificate ID printed on the physical badge or digital QR card.
                </p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
