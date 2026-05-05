import { initMobileDrawingScreen } from "../shared/mobileDrawingScreen.js";
import { SolutionSubmitBridge } from "../../../VotingClient/js/shared/solutionSubmitBridge.js";
import { startClientStateApp } from "../../../State Machine/Client State Machine/ClientStateApp.js";

window.clientStateMachine = startClientStateApp({ mode: "page" });

const { drawer, textInput, submitBtn } = initMobileDrawingScreen({
  textInputId: "solutionDescription"
});

const bridge = new SolutionSubmitBridge({
  drawer,
  descriptionInput: textInput,
  submitButton: submitBtn,
  fallbackDescription: "Something useful",
  fallbackPlayerName: "Unknown Hero",
  onSubmit: (payload) => {
    window.dispatchEvent(new CustomEvent("solution:submitted", { detail: payload }));
    console.log("[PhoneSolutionSubmit] submitted", payload.playerName);
  }
});

const query = new URLSearchParams(window.location.search);
const playerId = query.get("playerId");
const playerName = query.get("playerName");
if (playerId) {
  bridge.setLocalPlayer(playerId, playerName || "Unknown Hero");
}

bridge.init();
