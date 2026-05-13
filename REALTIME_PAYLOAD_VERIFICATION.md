# Realtime Image Messaging - Payload Verification Guide

## Payload Shape Alignment

### Production Hub (ConnectHub.ChatHub - Port 5003)

**Text Message Broadcast:**

```csharp
await Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", new {
    messageId,
    senderId,
    receiverId,
    content = cleanContent,
    sentAt = DateTime.UtcNow,
    messageType = "TEXT",
    isRead = false
});
```

**Media Message Broadcast:**

```csharp
await Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", new {
    messageId,
    senderId,
    receiverId,
    content = caption ?? string.Empty,
    mediaUrl = mediaUrl,
    sentAt = DateTime.UtcNow,
    messageType = "IMAGE",
    isRead = false
});
```

**Key Points:**
✅ camelCase field names  
✅ Single payload parameter (NOT user + payload)  
✅ mediaUrl included for image messages  
✅ messageType = "IMAGE" (uppercase)  
✅ Server logs with: `_logger.LogDebug("[Realtime Payload - Media] {Payload}", ...)`

---

### Test Hub (ConnectHub.Web - /hubs/chat)

**Updated Format (NOW ALIGNED):**

```csharp
await Clients.All.SendAsync("ReceiveMessage", new {
    senderId = user,
    receiverId = (string?)null,
    content = message,
    mediaUrl = mediaUrl,
    messageType = string.IsNullOrWhiteSpace(messageType) ? "TEXT" : messageType,
    sentAt = System.DateTime.UtcNow,
    isRead = false
});
```

**Changes Made:**
✅ Added `mediaUrl = mediaUrl` explicitly  
✅ Changed to send only payload (single param)  
✅ Now matches production hub signature  
✅ Server logs with: `Console.WriteLine($"[Test Hub] Broadcasting...")`

---

## Receiver (MVC Client)

**Handler (NOW ALIGNED):**

```javascript
connection.on("ReceiveMessage", (message) => {
  console.log("🔴 [RECEIVER] Raw ReceiveMessage payload:", message);
  console.log("  mediaUrl:", message?.mediaUrl);
  console.log("  messageType:", message?.messageType);
  console.log("  content:", message?.content);
  console.log("  senderId:", message?.senderId);

  const senderName = message?.senderId || "System";
  renderMessage(senderName, message);
});
```

**Normalization (handles both cases):**

```javascript
function normalizeMessage(message) {
  if (!message || typeof message !== "object") {
    return {
      content: typeof message === "string" ? message : "",
      mediaUrl: null,
      messageType: "text",
    };
  }

  return {
    ...message,
    messageType: (message.messageType || "text").toString().toLowerCase(),
    mediaUrl: message.mediaUrl || null,
    content: message.content || "",
  };
}
```

**Render Logic:**

```javascript
const isMediaMessage =
  !!normalized.mediaUrl || normalized.messageType === "image";

if (isMediaMessage) {
  if (normalized.mediaUrl) {
    const img = document.createElement("img");
    img.src = normalized.mediaUrl;
    img.style.maxWidth = "240px";
    img.style.display = "block";
    img.alt = "Shared image";
    container.appendChild(img);
  }

  if (normalized.content && normalized.content.trim()) {
    const caption = document.createElement("div");
    caption.textContent = normalized.content;
    container.appendChild(caption);
  }
} else {
  // Text message
  const msg = document.createElement("div");
  msg.textContent = normalized.content || JSON.stringify(normalized);
  container.appendChild(msg);
}
```

---

## Verification Checklist - Live Two-User Test

### Setup

- [ ] Both services running: `.\scripts\run-all-services.ps1`
- [ ] MVC chat page open: http://localhost/Chat/ViewRoomChat
- [ ] Browser DevTools open (F12)
- [ ] Console visible

### Sender Flow

1. **Upload Image**
   - [ ] Select image file
   - [ ] Verify backend: `POST http://localhost:5005/api/media/upload`
   - [ ] Expected response: `{ success: true, url: "http://localhost:5005/uploads/...", fileName: "..." }`
   - [ ] Server log: Media Service returns 200 OK

2. **Send Image Message via SignalR**
   - [ ] Enter image caption (or leave empty)
   - [ ] Click Send
   - [ ] Server log: `[Test Hub] Broadcasting ReceiveMessage: {...mediaUrl...}`
   - [ ] Sender console: Should NOT show red receiver logs (only broadcasts to all)
   - [ ] Sender sees image in chat ✅

### Receiver Flow (Critical Verification)

1. **Receive Event**
   - [ ] Receiver browser console shows: `🔴 [RECEIVER] Raw ReceiveMessage payload:`
   - [ ] Check following logs:
     - `mediaUrl: "http://localhost:5005/uploads/..."`
     - `messageType: "IMAGE"` OR `"image"` (both valid)
     - `content: ""` (or caption text)
     - `senderId: "<username>"`

2. **Message Normalization**
   - [ ] Verify `normalizeMessage()` received payload
   - [ ] Should convert `messageType` to lowercase
   - [ ] Should preserve `mediaUrl` as-is

3. **Image Renders**
   - [ ] Image appears in receiver chat (NOT blank bubble)
   - [ ] Image src is: `http://localhost:5005/uploads/...`
   - [ ] Caption appears below image (if provided)

---

## Property Name Mapping

### Backend → Frontend

| Backend (C#)  | Frontend (JS) | Type       | Required          |
| ------------- | ------------- | ---------- | ----------------- |
| `SenderId`    | `senderId`    | int/string | ✅                |
| `ReceiverId`  | `receiverId`  | int/string | ✅                |
| `Content`     | `content`     | string     | ✅                |
| `MediaUrl`    | `mediaUrl`    | string     | ❌ (null if text) |
| `MessageType` | `messageType` | string     | ✅                |
| `SentAt`      | `sentAt`      | datetime   | ✅                |
| `IsRead`      | `isRead`      | bool       | ✅                |

---

## Debugging Tips

### If Image Doesn't Render

1. **Check console for red receiver logs**
   - If no red logs: WebSocket connection not receiving (network issue)
   - If red logs present: Continue to step 2

2. **Check mediaUrl value**
   - Should be: `http://localhost:5005/uploads/[guid]_[filename]`
   - NOT: empty, null, or relative path
   - If null: Backend didn't include mediaUrl in payload

3. **Check img element creation**
   - If image created but blank: src is invalid
   - If image not created: `normalizeMessage()` returned `mediaUrl: null`

4. **Check messageType**
   - Should be: `'image'` (lowercase after normalization)
   - If `'TEXT'`: Image treated as text message (wrong branch)

### Server Logs to Check

- **Test Hub**: `[Test Hub] Broadcasting ReceiveMessage: {...}`
- **Production Hub**: `[Realtime Payload - Media] {...}`
- **Browser Console**: `🔴 [RECEIVER] Raw ReceiveMessage payload: {...}`

---

## Success Indicators

When working correctly, you should see:

**Server Console:**

```
[Test Hub] Broadcasting ReceiveMessage: {"senderId":"User1","receiverId":null,"content":"","mediaUrl":"http://localhost:5005/uploads/abc123_photo.png","messageType":"IMAGE","sentAt":"2026-05-10T...","isRead":false}
```

**Browser Console (Receiver):**

```
🔴 [RECEIVER] Raw ReceiveMessage payload: {
  senderId: "User1",
  receiverId: null,
  content: "",
  mediaUrl: "http://localhost:5005/uploads/abc123_photo.png",
  messageType: "IMAGE",
  sentAt: "2026-05-10T...",
  isRead: false
}
  mediaUrl: http://localhost:5005/uploads/abc123_photo.png
  messageType: IMAGE
  content: (empty string)
  senderId: User1
```

**Visual (Receiver Chat):**

```
User1:
[image displayed inline - 240px max width]
```

---

## Files Modified This Session

1. **ConnectHub.Web/Hubs/ChatHub.cs**
   - Changed from 2-param broadcast to 1-param
   - Now sends only payload object
   - Added explicit mediaUrl field
   - Added console logging

2. **ConnectHub.Web/Views/Chat/ViewRoomChat.cshtml**
   - Changed handler signature from `(user, message)` to `(message)`
   - Added detailed console logging with 🔴 indicator
   - Extracts senderId from payload
   - Passes normalized payload to render

3. **ConnectHub.ChatHub/Hubs/ChatHub.cs** (No changes - already aligned)
   - Already sends 1-param format
   - Already uses camelCase
   - Already includes mediaUrl

---

## Next Steps

1. **Run Live Test**

   ```bash
   # Terminal 1: Start services
   .\scripts\run-all-services.ps1

   # Terminal 2: Monitor server logs
   dotnet run --project ConnectHub.ChatHub/ConnectHub.ChatHub.csproj
   ```

2. **Open Two Browsers**
   - Browser 1 (Sender): http://localhost/Chat/ViewRoomChat
   - Browser 2 (Receiver): http://localhost/Chat/ViewRoomChat (different window/incognito)

3. **Send Image**
   - Sender: Enter name "User1"
   - Sender: Type image caption or leave empty
   - Sender: Invoke `SendMessage("User1", "", "http://localhost:5005/uploads/...", "IMAGE")`
   - Check both consoles for logs

4. **Verify Payload**
   - Receiver console should show: `🔴 [RECEIVER]...mediaUrl: http://...`
   - Image should render instantly

---

## Status

**Last Updated:** May 10, 2026  
**Backend Hub Payload:** ✅ camelCase, single-param  
**Test Hub Payload:** ✅ camelCase, single-param (ALIGNED)  
**Receiver Handler:** ✅ Single-param signature, detailed logging  
**Normalization:** ✅ Handles all variations  
**Image Rendering:** ✅ Checks mediaUrl existence

**Ready for Verification Test**: YES
