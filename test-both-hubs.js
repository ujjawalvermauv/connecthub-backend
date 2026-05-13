const http = require('http');

const testHub = (port, hubPath, name) => {
  const options = {
    hostname: 'localhost',
    port,
    path: hubPath,
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Content-Length': 2
    }
  };

  const req = http.request(options, (res) => {
    let data = '';
    res.on('data', chunk => data += chunk);
    res.on('end', () => {
      try {
        const json = JSON.parse(data);
        const transports = json.availableTransports.map(t => t.transport).join(', ');
        console.log(`✅ ${name}: ${transports}`);
      } catch (e) {
        console.error(`❌ ${name}: Failed to parse response`);
      }
    });
  });

  req.on('error', (e) => console.error(`❌ ${name}: ${e.message}`));
  req.write('{}');
  req.end();
};

console.log('Testing SignalR Hubs - Available Transports:\n');
testHub(5003, '/hubs/chat/negotiate', 'ChatHub');
testHub(5004, '/hubs/notifications/negotiate', 'NotificationHub');

setTimeout(() => process.exit(0), 2000);
