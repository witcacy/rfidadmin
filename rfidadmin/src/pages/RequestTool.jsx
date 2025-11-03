import React from 'react';

export default function RequestTool() {
    return (
        <div>
            <h4 className="mb-3">Request Tool</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Reason for Request</label>
                    <select name="reasonforrequest" className="form-select">
                        <option value="Option 1">Option 1</option>
                        <option value="Option 2">Option 2</option>
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Area</label>
                    <select name="area" className="form-select">
                        <option value="Option 1">Option 1</option>
                        <option value="Option 2">Option 2</option>
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Tool Type</label>
                    <select name="tooltype" className="form-select">
                        <option value="Option 1">Option 1</option>
                        <option value="Option 2">Option 2</option>
                    </select>
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-primary">
                        <i className="bi bi-send me-2"></i> Submit Ticket
                    </button>
                    <button type="button" className="btn btn-secondary">
                        <i className="bi bi-plus-circle me-2"></i> New Ticket
                    </button>
                    <button type="button" className="btn btn-outline-dark">
                        <i className="bi bi-arrow-left-circle me-2"></i> Return to Menu
                    </button>
                </div>
            </form>
        </div>
    );
}