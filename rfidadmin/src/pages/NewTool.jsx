import React from 'react';

export default function NewTool() {
    return (
        <div>
            <h4 className="mb-3">Add New Tool</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Tool Type</label>
                    <select name="tooltype" className="form-select">
                        <option value="Option 1">Option 1</option>
                        <option value="Option 2">Option 2</option>
                    </select>
                </div>
                <div className="mb-3">
                    <label className="form-label">Serial Number</label>
                    <input type="text" name="serialnumber" className="form-control" />
                </div>
                <div className="mb-3">
                    <label className="form-label">Description</label>
                    <input type="text" name="description" className="form-control" />
                </div>
                <div className="mb-3">
                    <label className="form-label">RFID Read</label>
                    <input type="text" name="rfidread" className="form-control" readOnly />
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-success">
                        <i className="bi bi-plus-circle me-2"></i> Add Tool
                    </button>
                    <button type="button" className="btn btn-secondary">
                        <i className="bi bi-plus-square me-2"></i> Add Another Tool
                    </button>
                    <button type="button" className="btn btn-outline-dark">
                        <i className="bi bi-arrow-left-circle me-2"></i> Return to Menu
                    </button>
                </div>
            </form>
        </div>
    );
}