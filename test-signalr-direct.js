const { HubConnectionBuilder, LogLevel } = require("@microsoft/signalr");

async function testSignalRConnection() {
    console.log('Testing direct SignalR connection (no Angular)...\n');

    // For now, test without token to see if the basic connection works
    const connection = new HubConnectionBuilder()
        .withUrl("http://localhost:5003/hubs/chat", {
            // accessTokenFactory: () =>  token here
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();

    connection.onreconnecting(() => {
        console.log('[TEST] Reconnecting...');
    });

    connection.onreconnected(() => {
        console.log('[TEST] Reconnected');
    });

    connection.onclose(() => {
        console.log('[TEST] Connection closed');
    });

    try {
        await connection.start();
        console.log('[TEST] ✅ Connection started successfully!');
        console.log('[TEST] Connection ID:', connection.connectionId);
        console.log('[TEST] Connection State:', connection.state);

        // Try to call a method
        try {
            await connection.invoke("GetConnectedUsers");
            console.log('[TEST] ✅ Successfully invoked a hub method!');
        } catch (e) {
            console.log('[TEST] Method call error:', e.message);
        }

        // Keep connection alive for 5 seconds then close
        await new Promise(resolve => setTimeout(resolve, 5000));
        await connection.stop();
        console.log('[TEST] Connection stopped');
    } catch (err) {
        console.error('[TEST] ❌ Connection failed:', err.message);
        process.exit(1);
    }
}

testSignalRConnection();
