import React from 'react';

export default function RemoveTool() {
    return (
        <form className="p-3 border rounded bg-light">
            <div className="mb-3">
                <label className="form-label">Reason for Request</label>
                <select name="reasonforrequest" className="form-select">
                    <option value="Option 1">Option 1</option>
                    <option value="Option 2">Option 2</option>
                </select>
            </div>

            <div className="mb-3">
                <label className="form-label">RFID Read</label>
                <input type="text" name="rfidread" readOnly className="form-control" />
            </div>

            <div className="d-flex gap-2">
                <button type="button" className="btn btn-danger">Remove Another Tool</button>
                <button type="button" className="btn btn-secondary">Return to Menu</button>
            </div>
        </form>
    );
}