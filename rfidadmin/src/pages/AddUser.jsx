import React, { useState, useEffect } from 'react';
import { usersApi, rolesApi } from '../api/apiClient';

export default function AddUser() {
    const [roles, setRoles] = useState([]);
    const [form, setForm] = useState({ fullName: '', employeeId: '', badgeId: '', department: '', roleId: '' });
    const [message, setMessage] = useState(null);

    useEffect(() => {
        rolesApi.getAll()
            .then(setRoles)
            .catch(() => setMessage({ type: 'danger', text: 'Error loading roles.' }));
    }, []);

    const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage(null);
        try {
            await usersApi.create({
                fullName: form.fullName,
                employeeId: form.employeeId,
                badgeId: form.badgeId,
                department: form.department,
                roleId: Number(form.roleId),
            });
            setMessage({ type: 'success', text: 'User registered successfully.' });
            setForm({ fullName: '', employeeId: '', badgeId: '', department: '', roleId: '' });
        } catch (err) {
            setMessage({ type: 'danger', text: err.message || 'Error registering user.' });
        }
    };

    return (
        <div>
            <h4 className="mb-3">Add New User</h4>
            {message && <div className={`alert alert-${message.type}`}>{message.text}</div>}
            <form onSubmit={handleSubmit}>
                <div className="mb-3">
                    <label className="form-label">Full Name</label>
                    <input type="text" name="fullName" className="form-control" placeholder="Enter full name" value={form.fullName} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Employee ID</label>
                    <input type="text" name="employeeId" className="form-control" placeholder="Enter employee ID" value={form.employeeId} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Badge ID</label>
                    <input type="text" name="badgeId" className="form-control" placeholder="Enter badge ID" value={form.badgeId} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Department</label>
                    <input type="text" name="department" className="form-control" placeholder="Enter department" value={form.department} onChange={handleChange} />
                </div>
                <div className="mb-3">
                    <label className="form-label">Role</label>
                    <select name="roleId" className="form-select" value={form.roleId} onChange={handleChange}>
                        <option value="">-- Select --</option>
                        {roles.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                    </select>
                </div>
                <button type="submit" className="btn btn-primary">
                    <i className="bi bi-person-plus me-2"></i> Register User
                </button>
            </form>
        </div>
    );
}