// Create main joinScreen
const joinScreen = document.createElement("div");
//joinScreen.style.display = "none"
joinScreen.style.textAlign = "center";
joinScreen.style.marginTop = "20vh";
joinScreen.style.fontFamily = "sans-serif";
joinScreen.style.color = "white";
document.body.style.backgroundColor = "#3a1f00";
document.body.appendChild(joinScreen);

// Title
const title = document.createElement("h1");
title.textContent = "Enter Room Code";
joinScreen.appendChild(title);

// Input joinScreen
const inputWrapper = document.createElement("div");
joinScreen.appendChild(inputWrapper);

const inputs = [];
const CODE_LENGTH = 5;

// Create input boxes
for (let i = 0; i < CODE_LENGTH; i++) {
  const input = document.createElement("input");

  input.maxLength = 1;
  input.inputMode = "numeric"; // Show numeric keyboard on mobile
  input.pattern = "[0-9]*"; // Only allow digits
  input.style.width = "50px";
  input.style.height = "60px";
  input.style.margin = "5px";
  input.style.fontSize = "28px";
  input.style.textAlign = "center";
  input.style.borderRadius = "10px";
  input.style.border = "none";

  // Auto focus next
  input.addEventListener("input", () => {
    input.value = input.value.replace(/[^0-9]/g, ""); // Remove non-digits

    if (input.value && i < CODE_LENGTH - 1) {
      inputs[i + 1].focus();
    }
  });

  // Handle backspace
  input.addEventListener("keydown", (e) => {
    if (e.key === "Backspace" && !input.value && i > 0) {
      inputs[i - 1].focus();
    }
  });

  inputs.push(input);
  inputWrapper.appendChild(input);
}

// Join button
const button = document.createElement("button");
button.textContent = "Join Room";

button.style.marginTop = "20px";
button.style.padding = "15px 40px";
button.style.fontSize = "18px";
button.style.borderRadius = "12px";
button.style.background = "#22c55e";
button.style.color = "white";
button.style.border = "none";

joinScreen.appendChild(button);

// Status text
const status = document.createElement("p");
status.style.marginTop = "15px";
joinScreen.appendChild(status);

// Button click
button.onclick = () => {
  const code = inputs.map(i => i.value).join("");

  if (code.length < CODE_LENGTH || code != "12345") {
    status.textContent = "Room code invalid";
    status.style.color = "red";
    return;
  }

  status.textContent = "Joined room successfully!";
  status.style.color = "lightgreen";
};

// Auto focus first input on load
inputs[0].focus();