const STORAGE_KEY = "dd-client-session";
const DEFAULT_WS_URL = "wss://doodledrifterserver-production.up.railway.app";

export function getClientSession() {
  if (typeof sessionStorage === "undefined") {
    return { playerId: "", playerName: "" };
  }

  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return { playerId: "", playerName: "" };
  }

  try {
    const parsed = JSON.parse(raw);
    return {
      playerId: parsed.playerId || "",
      playerName: parsed.playerName || ""
    };
  } catch {
    return { playerId: "", playerName: "" };
  }
}

export function setClientSession(update) {
  if (typeof sessionStorage === "undefined") {
    return;
  }

  const current = getClientSession();
  const next = {
    playerId: update.playerId || current.playerId || "",
    playerName: update.playerName || current.playerName || ""
  };

  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
}

export function resolveServerUrl() {
  if (typeof window !== "undefined") {
    if (window.DD_WS_URL) {
      return window.DD_WS_URL;
    }

    const params = new URLSearchParams(window.location.search);
    const paramUrl = params.get("ws");
    if (paramUrl) {
      return paramUrl;
    }
  }

  return DEFAULT_WS_URL;
}

export function createClientSocket(options = {}) {
  const {
    onOpen,
    onMessage,
    onClose,
    onError
  } = options;

  const ws = new WebSocket(resolveServerUrl());

  ws.onopen = () => {
    if (typeof onOpen === "function") {
      onOpen();
    }
  };

  ws.onmessage = (event) => {
    if (!event || !event.data) return;

    let message = null;
    try {
      message = JSON.parse(event.data);
    } catch {
      return;
    }

    if (typeof onMessage === "function") {
      onMessage(message);
    }
  };

  ws.onerror = (event) => {
    if (typeof onError === "function") {
      onError(event);
    }
  };

  ws.onclose = (event) => {
    if (typeof onClose === "function") {
      onClose(event);
    }
  };

  return {
    socket: ws,
    send(payload) {
      if (ws.readyState !== WebSocket.OPEN) {
        return;
      }
      ws.send(JSON.stringify(payload));
    },
    close() {
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        ws.close();
      }
    }
  };
}
