// File download helper
window.downloadFile = function (filename, base64Content) {
    const link = document.createElement('a');
    link.download = filename;

    // Determine MIME type based on file extension
    let mimeType = 'application/octet-stream';
    if (filename.endsWith('.csv')) {
        mimeType = 'text/csv';
    } else if (filename.endsWith('.xlsx')) {
        mimeType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    } else if (filename.endsWith('.pdf')) {
        mimeType = 'application/pdf';
    }

    link.href = 'data:' + mimeType + ';base64,' + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
