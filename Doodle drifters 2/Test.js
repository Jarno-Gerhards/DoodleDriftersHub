const WebSocket = require("ws");

// =====================
// CONFIG
// =====================
const SERVER_URL = "wss://doodledrifterserver-production.up.railway.app";

// Fake phone player identity
const player = {
    id: "player_" + Math.floor(Math.random() * 10000),
    name: "Player_" + Math.floor(Math.random() * 1000)
};

// =====================
// CONNECT TO SERVER
// =====================
const ws = new WebSocket(SERVER_URL);

// =====================
// CONNECTION OPEN
// =====================
ws.on("open", () => {
    console.log("CONNECTED TO SERVER");

    ws.send(JSON.stringify({
        type: "JOIN",
        name: "TestPlayer"
    }));
});

ws.on("error", (err) => {
    console.error("CONNECTION ERROR:", err.message);
});

ws.on("close", () => {
    console.log("CONNECTION CLOSED");
});

// =====================
// RECEIVE MESSAGES (FROM UNITY VIA SERVER)
// =====================
ws.on("message", (data) => {
    const msg = JSON.parse(data.toString());

    console.log("FROM SERVER:", msg);

    handleServerMessage(msg);
});

// =====================
// PHONE STATE MACHINE (UI STATE MIRROR)
// =====================
let state = "LOBBY";

function handleServerMessage(msg) {

    switch (msg.type) {

        case "STATE":
            setState(msg.state);
            break;

        case "START_DRAW":
            setState("Draw");
            break;

        case "START_VOTE":
            setState("Vote");
            break;
    }
}

// =====================
// STATE HANDLER (UI LOGIC)
// =====================
function setState(newState) {

    state = newState;

    console.log("PHONE STATE →", state);

    switch (state) {

        case "Lobby":
            enterLobby();
            break;

        case "Draw":
            enterDrawMode();
            break;

        case "Vote":
            enterVoteMode();
            break;
    }
}

// =====================
// UI STATE FUNCTIONS (STUBS)
// =====================
function enterLobby() {
    console.log("Waiting for game to start...");
}

function enterDrawMode() {
    console.log("Draw mode active");

    // Example auto-test drawing (REMOVE IN REAL APP)
    setTimeout(() => {
        submitDrawing("fake_base64_image_data");
    }, 3000);
}

function enterVoteMode() {
    console.log("Vote mode active");

    // Example auto-test vote (REMOVE IN REAL APP)
    setTimeout(() => {
        vote("drawing_123");
    }, 2000);
}

// =====================
// PLAYER ACTIONS
// =====================

// Send drawing to Unity via server
function submitDrawing(imageData) {

    console.log("Sending drawing...");

    ws.send(JSON.stringify({
        type: "SUBMIT_DRAWING",
        playerId: player.id,
        image: imageData
    }));
}

// Send vote to Unity via server
function vote(drawingId) {

    console.log("Sending vote...");

    ws.send(JSON.stringify({
        type: "VOTE",
        playerId: player.id,
        drawingId: drawingId
    }));
}

// =====================
// SAFETY LOGGING
// =====================
ws.on("close", () => {
    console.log("Disconnected from server");
});

ws.on("error", (err) => {
    console.error("WebSocket error:", err.message);
});