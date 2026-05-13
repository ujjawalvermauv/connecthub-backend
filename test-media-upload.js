// Test Media Service upload endpoint
const FormData = require('form-data');
const fs = require('fs');
const path = require('path');
const http = require('http');

// Create a simple test image (1x1 pixel PNG)
const testImagePath = path.join(__dirname, 'test-image.png');
const pngBuffer = Buffer.from([
    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
    0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
    0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
    0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
    0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
    0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
]);

fs.writeFileSync(testImagePath, pngBuffer);

// Create form data
const form = new FormData();
form.append('file', fs.createReadStream(testImagePath));

// Make request
const options = {
    hostname: 'localhost',
    port: 5005,
    path: '/api/media/upload',
    method: 'POST',
    headers: form.getHeaders()
};

console.log('\n📤 Testing Media Service upload endpoint...\n');
console.log(`POST http://localhost:5005/api/media/upload`);
console.log(`Content-Type: multipart/form-data`);
console.log(`File: test-image.png\n`);

const req = http.request(options, (res) => {
    let data = '';

    res.on('data', (chunk) => {
        data += chunk;
    });

    res.on('end', () => {
        console.log(`✅ Response Status: ${res.statusCode}`);
        console.log(`Response Headers:`);
        console.log(`  Content-Type: ${res.headers['content-type']}`);
        console.log(`\nResponse Body:`);
        console.log(JSON.stringify(JSON.parse(data), null, 2));

        if (res.statusCode === 200) {
            const response = JSON.parse(data);
            console.log(`\n✅ SUCCESS!`);
            console.log(`   Image URL: ${response.url}`);
            console.log(`   File Name: ${response.fileName}`);
        } else {
            console.log(`\n❌ FAILED with status ${res.statusCode}`);
        }

        // Cleanup
        fs.unlinkSync(testImagePath);
    });
});

req.on('error', (e) => {
    console.error(`❌ Request Error: ${e.message}`);
    fs.unlinkSync(testImagePath);
});

form.pipe(req);
