import React, { useState } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';
import NewTool from './Pages/NewTool';
import RequestTool from './Pages/RequestTool';
import AssignTool from './Pages/AssignTool';
import RemoveTool from './Pages/RemoveTool';
import MaintenanceRequired from './Pages/MaintenanceRequired';
import ReportViewer from './Pages/ReportViewer';
import AddUser from './Pages/AddUser';           // Nuevo componente
import RFIDScan from './Pages/RFIDScan';         // Nuevo componente
import TicketSystem from './Pages/TicketSystem'; // Nuevo componente

export default function App() {
    const [activePage, setActivePage] = useState('');

    const renderPage = () => {
        switch (activePage) {
            case 'new': return <NewTool />;
            case 'request': return <RequestTool />;
            case 'assign': return <AssignTool />;
            case 'remove': return <RemoveTool />;
            case 'maintenance': return <MaintenanceRequired />;
            case 'report': return <ReportViewer />;
            case 'adduser': return <AddUser />;
            case 'rfid': return <RFIDScan />;
            case 'ticket': return <TicketSystem />;
            default: return <h4 className="text-center mt-4">Selecciona una opción del menú</h4>;
        }
    };

    return (
        <div className="container mt-4">
            <h1 className="text-center mb-4">RFID Web Tool</h1>
            <div className="d-flex flex-wrap justify-content-center gap-2 mb-4">
                <button className="btn btn-primary" onClick={() => setActivePage('new')}>
                    <i className="bi bi-plus-circle me-2"></i> New Tool
                </button>
                <button className="btn btn-secondary" onClick={() => setActivePage('request')}>
                    <i className="bi bi-ticket me-2"></i> Request Tool
                </button>
                <button className="btn btn-success" onClick={() => setActivePage('assign')}>
                    <i className="bi bi-person-check me-2"></i> Assign Tool
                </button>
                <button className="btn btn-danger" onClick={() => setActivePage('remove')}>
                    <i className="bi bi-trash me-2"></i> Remove Tool
                </button>
                <button className="btn btn-warning" onClick={() => setActivePage('maintenance')}>
                    <i className="bi bi-tools me-2"></i> Maintenance Required
                </button>
                <button className="btn btn-info" onClick={() => setActivePage('report')}>
                    <i className="bi bi-bar-chart me-2"></i> Report Viewer
                </button>
                <button className="btn btn-dark" onClick={() => setActivePage('adduser')}>
                    <i className="bi bi-person-plus me-2"></i> Add User
                </button>
                <button className="btn btn-outline-primary" onClick={() => setActivePage('rfid')}>
                    <i className="bi bi-upc-scan me-2"></i> RFID Scan
                </button>
                <button className="btn btn-outline-warning" onClick={() => setActivePage('ticket')}>
                    <i className="bi bi-journal-text me-2"></i> Ticket System
                </button>
            </div>

            <div className="text-center mb-3">
                <button className="btn btn-outline-dark me-2" onClick={() => setActivePage('')}>
                    <i className="bi bi-arrow-left-circle me-2"></i> Return to Menu
                </button>
                <button className="btn btn-outline-success">
                    <i className="bi bi-file-earmark-excel me-2"></i> Export to Excel
                </button>
            </div>

            <div className="card p-3">
                {renderPage()}
            </div>
        </div>
    );
}