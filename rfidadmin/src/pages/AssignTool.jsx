
export default function AssignTool() {
    return (
        <div>
            <h4 className="mb-3">Assign Tool</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Scan Badge</label>
                    <input type="text" name="scanbadge" className="form-control" placeholder="Enter badge ID" />
                </div>
                <div className="mb-3">
                    <label className="form-label">RFID Read</label>
                    <input type="text" name="rfidread" className="form-control" readOnly placeholder="Waiting for RFID..." />
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-outline-dark">
                        <i className="bi bi-arrow-left-circle me-2"></i> Return to Menu
                    </button>
                    <button type="button" className="btn btn-outline-secondary">
                        <i className="bi bi-ticket me-2"></i> Return to Tickets
                    </button>
                </div>
            </form>
        </div>
    );
}