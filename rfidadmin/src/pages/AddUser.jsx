import React from 'react';

export default function AddUser() {
    return (
        <div>
            <h4 className="mb-3">Add New User</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Full Name</label>
                    <input type="text" className="form-control" placeholder="Enter full name" />
                </div>
                <div className="mb-3">
                    <label className="form-label">Employee ID</label>
                    <input type="text" className="form-control" placeholder="Enter employee ID" />
                </div>
                <div className="mb-3">
                    <label className="form-label">Department</label>
                    <input type="text" className="form-control" placeholder="Enter department" />
                </div>
                <button type="submit" className="btn btn-primary">
                    <i className="bi bi-person-plus me-2"></i> Register User
                </button>
            </form>
        </div>
    );
}