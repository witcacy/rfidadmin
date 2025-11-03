import React from 'react';

export default function TicketSystem() {
    return (
        <div>
            <h4 className="mb-3">Ticket System</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Issue Title</label>
                    <input type="text" className="form-control" placeholder="Enter issue title" />
                </div>
                <div className="mb-3">
                    <label className="form-label">Description</label>
                    <textarea className="form-control" rows="4" placeholder="Describe the issue..."></textarea>
                </div>
                <div className="mb-3">
                    <label className="form-label">Priority</label>
                    <select className="form-select">
                        <option>Low</option>
                        <option>Medium</option>
                        <option>High</option>
                    </select>
                </div>
                <button type="submit" className="btn btn-warning">
                    <i className="bi bi-send me-2"></i> Submit Ticket
                </button>
            </form>
        </div>
    );
}