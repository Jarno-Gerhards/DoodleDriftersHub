import { initMobileDrawingScreen } from "../shared/mobileDrawingScreen.js";
import { createSubmissionPayload, dispatchSubmission } from "../core/gameLogic.js";

const fallbackName = "Unnamed Champion";

const { drawer, textInput: championNameInput, submitBtn } = initMobileDrawingScreen({
  textInputId: "championName"
});

submitBtn.addEventListener("click", () => {
  const payload = createSubmissionPayload({
    drawer,
    championNameInput: championNameInput ? championNameInput.value : "",
    fallbackName
  });

  dispatchSubmission(payload);
  console.log("[PhoneDrawingSubmit] submitted", payload.championName);
});
