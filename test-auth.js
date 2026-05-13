const http = require('http');

function requestJson(options, body) {
    return new Promise((resolve, reject) => {
        const req = http.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => {
                data += chunk;
            });
            res.on('end', () => {
                let parsed = data;
                try {
                    parsed = data ? JSON.parse(data) : null;
                } catch {
                    // keep raw string
                }
                resolve({ statusCode: res.statusCode, body: parsed });
            });
        });

        req.on('error', reject);
        if (body) {
            req.write(JSON.stringify(body));
        }
        req.end();
    });
}

async function run() {
    console.log('\nTesting auth profile endpoint flow...');

    const loginResponse = await requestJson(
        {
            hostname: 'localhost',
            port: 5001,
            path: '/api/users/login',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        },
        {
            email: 'shubham@gmail.com',
            password: '1234567890'
        }
    );

    console.log('Login Response:', loginResponse.statusCode);
    if (loginResponse.statusCode !== 200 || !loginResponse.body?.token) {
        console.log('Login Body:', loginResponse.body);
        return;
    }

    const profileResponse = await requestJson({
        hostname: 'localhost',
        port: 5001,
        path: '/api/users/profile',
        method: 'GET',
        headers: {
            Authorization: `Bearer ${loginResponse.body.token}`
        }
    });

    console.log('Profile Endpoint Response:', profileResponse.statusCode);
    console.log('Profile Body:', profileResponse.body);
}

run().catch((err) => {
    console.error('Error:', err.message);
});
