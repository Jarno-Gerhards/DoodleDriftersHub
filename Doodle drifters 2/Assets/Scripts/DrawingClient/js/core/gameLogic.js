export function resolveChampionName(rawValue, fallbackName = "Unnamed Champion") {
  const value = String(rawValue || "").trim();
  return value.length > 0 ? value : fallbackName;
}

export function createSubmissionPayload({
  drawer,
  championNameInput,
  fallbackName = "Unnamed Champion"
}) {
  const championName = resolveChampionName(championNameInput, fallbackName);

  return {
    championName,
    imageDataUrl: drawer.getTextureWithTransparentBackground(),
    submittedAt: new Date().toISOString()
  };
}

export function dispatchSubmission(payload, eventName = "drawing:submitted") {
  window.dispatchEvent(new CustomEvent(eventName, { detail: payload }));
}
