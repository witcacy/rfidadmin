import React from 'react';

export default function RFIDScan() {
    return (
        <div>
            <h4 className="mb-3">RFID Scan</h4>
            <p>Waiting for RFID tag scan...</p>
            <div className="alert alert-info">
                <i className="bi bi-upc-scan me-2"></i> No tag detected yet.
            </div>
            {/* Aquí puedes agregar lógica para mostrar datos del tag escaneado */}
        </div>
    );
}