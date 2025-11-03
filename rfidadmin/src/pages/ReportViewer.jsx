

export default function ReportViewer() {
    return (
        <div>
            <h4 className="mb-3">Report Viewer</h4>
            <form>
                <div className="mb-3">
                    <label className="form-label">Start Date</label>
                    <input type="date" name="startdate" className="form-control" />
                </div>
                <div className="mb-3">
                    <label className="form-label">End Date</label>
                    <input type="date" name="enddate" className="form-control" />
                </div>
                <div className="mb-3">
                    <label className="form-label">Status</label>
                    <select name="status" className="form-select">
                        <option value="Open">Open</option>
                        <option value="Closed">Closed</option>
                        <option value="All">All</option>
                    </select>
                </div>
                <div className="d-flex gap-2">
                    <button type="button" className="btn btn-primary">
                        <i className="bi bi-bar-chart-line me-2"></i> Run Report
                    </button>
                    <button type="button" className="btn btn-success">
                        <i className="bi bi-file-earmark-excel me-2"></i> Export to Excel
                    </button>
                </div>
            </form>
        </div>
    );
}