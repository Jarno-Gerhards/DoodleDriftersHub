import StateMachine from "./Assets/Scripts/State Machine/Client State Machine/StateMachine.js";

const sm = new StateMachine();
sm.start();

// simulate update loop
for (let i = 0; i < 5; i++) {
    sm.update();
}

// test transitions
sm.switchState("Drawing");
sm.update();

sm.switchState("Voting");
sm.update();