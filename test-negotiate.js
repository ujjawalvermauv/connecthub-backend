const http = require('http');

console.log('Testing SignalR Negotiate Endpoint...\n');

// Test ChatHub negotiate
const options = {
  hostname: 'localhost',
  port: 5003,
  path: '/hubs/chat/negotiate',
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Content-Length': 2
  }
};

const req = http.request(options, (res) => {
  console.log(`ChatHub Negotiate Response - Status: ${res.statusCode}`);
  console.log('Headers:');
  Object.keys(res.headers).forEach(key => {
    console.log(`  ${key}: ${res.headers[key]}`);
  });

  let data = '';
  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    try {
      const json = JSON.parse(data);
      console.log('\nResponse Body:');
      console.log(JSON.stringify(json, null, 2));
      
      if (json.availableTransports) {
        console.log('\n✅ Available Transports:');
        json.availableTransports.forEach(transport => {
          console.log(`   - ${transport}`);
        });
      }
    } catch (e) {
      console.log('Response:', data);
    }
  });
});

req.on('error', (error) => {
  console.error(`Error: ${error.message}`);
});

req.write('{}');
req.end();
