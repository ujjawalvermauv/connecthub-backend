const http = require('http');

const ports = [5000, 5001, 5002, 5003, 5076, 5078];
const portChecks = ports.map(port => {
    return new Promise(resolve => {
        const req = http.get(`http://localhost:${port}/`, { timeout: 1000 }, (res) => {
            resolve({ port, status: `OK (${res.statusCode})` });
        });
        req.on('error', (err) => {
            resolve({ port, status: `NOT RESPONDING (${err.code})` });
        });
        req.on('timeout', () => {
            req.destroy();
            resolve({ port, status: 'TIMEOUT' });
        });
    });
});

Promise.all(portChecks).then(results => {
    console.log('\n=== BACKEND SERVICE PORT STATUS ===\n');
    results.forEach(r => {
        console.log(`Port ${r.port}: ${r.status}`);
    });
});
