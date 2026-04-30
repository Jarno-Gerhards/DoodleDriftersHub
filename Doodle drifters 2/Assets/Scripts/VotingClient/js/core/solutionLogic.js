export function resolveSolutionDescription(rawValue, fallbackValue = "Something useful") {
  const value = String(rawValue || "").trim();
  return value.length > 0 ? value : fallbackValue;
}

function resolveDrawingData(drawer) {
  if (!drawer) {
    throw new Error("[SolutionLogic] drawer is required");
  }

  if (typeof drawer.getTextureWithTransparentBackground === "function") {
    return drawer.getTextureWithTransparentBackground();
  }

  if (drawer instanceof HTMLCanvasElement) {
    return drawer.toDataURL("image/png");
  }

  if (drawer.canvas instanceof HTMLCanvasElement) {
    return drawer.canvas.toDataURL("image/png");
  }

  throw new Error("[SolutionLogic] unsupported drawer type");
}

export function createSolutionSubmissionPayload({
  drawer,
  descriptionInput,
  fallbackDescription = "Something useful",
  playerId,
  playerName = "Unknown Hero"
}) {
  return {
    playerId,
    playerName,
    description: resolveSolutionDescription(descriptionInput, fallbackDescription),
    imageDataUrl: resolveDrawingData(drawer),
    submittedAt: new Date().toISOString()
  };
}

export function dispatchSolutionSubmission(payload, eventName = "solution:submitted") {
  window.dispatchEvent(new CustomEvent(eventName, { detail: payload }));
}
