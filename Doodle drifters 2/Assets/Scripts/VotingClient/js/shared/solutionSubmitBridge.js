import {
  createSolutionSubmissionPayload,
  dispatchSolutionSubmission
} from "../core/solutionLogic.js";

export class SolutionSubmitBridge {
  constructor(options) {
    const {
      drawer,
      descriptionInput,
      submitButton,
      fallbackDescription = "Something useful",
      fallbackPlayerName = "Unknown Hero",
      onSubmit = null
    } = options || {};

    if (!drawer) {
      throw new Error("[SolutionSubmitBridge] drawer is required");
    }

    this.drawer = drawer;
    this.descriptionInput = descriptionInput || null;
    this.submitButton = submitButton || null;
    this.fallbackDescription = fallbackDescription;
    this.playerId = "web-local-player";
    this.playerName = fallbackPlayerName;
    this.onSubmitCallback = onSubmit;
  }

  setLocalPlayer(playerId, playerName) {
    this.playerId = playerId;
    this.playerName = playerName || this.playerName;
  }

  init() {
    if (!this.submitButton) {
      return;
    }

    this.submitButton.addEventListener("click", () => {
      const payload = this.onSubmit();
      if (payload) {
        console.log("[SolutionSubmitBridge] Submitted:", payload.playerName);
      }
    });
  }

  onSubmit() {
    const payload = createSolutionSubmissionPayload({
      drawer: this.drawer,
      descriptionInput: this.descriptionInput ? this.descriptionInput.value : "",
      fallbackDescription: this.fallbackDescription,
      playerId: this.playerId,
      playerName: this.playerName
    });

    if (typeof this.onSubmitCallback === "function") {
      this.onSubmitCallback(payload);
    } else {
      dispatchSolutionSubmission(payload);
    }

    return payload;
  }
}
