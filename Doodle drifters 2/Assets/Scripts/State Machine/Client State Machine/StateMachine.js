import GameContext from "./GameContext.js";
import State from "./State.js";
import LobbyState from "./States/LobbyState.js"
import TestState from "./States/TestState.js"

/**
 * Enum replacement
 */
export const GameStateType = Object.freeze({
    LOBBY: "Lobby",
    DRAWING: "Drawing",
    VOTING: "Voting"
});

export default class StateMachine {
    constructor() {
        this.states = new Map();
        this.validTransitions = new Map([
            [GameStateType.LOBBY, [GameStateType.DRAWING]],
            [GameStateType.DRAWING, [GameStateType.VOTING]],
            [GameStateType.VOTING, [GameStateType.DRAWING]]
        ]);

        this.currentState = null;
        this.currentStateType = null;
        this.context = new GameContext(this);

        this.initializeStates();
    }

    initializeStates() {
        // Replace these with your real state implementations
        this.states.set(GameStateType.LOBBY, new LobbyState());
        this.states.set(GameStateType.DRAWING, new TestState());
        this.states.set(GameStateType.VOTING, new TestState());
    }

    start() {
        this.switchState(GameStateType.LOBBY);
    }

    update() {
        if (this.currentState) {
            this.currentState.update(this.context);
        }
    }

    fixedUpdate() {
        if (this.currentState) {
            this.currentState.fixedUpdate(this.context);
        }
    }

    switchState(newState) {
        // Enable this when transitions are enforced
        /*
        const allowed = this.validTransitions.get(this.currentStateType);
        if (allowed && !allowed.includes(newState)) {
            console.warn("Invalid transition");
            return;
        }
        */

        this.currentStateType = newState;
        this.setState(this.states.get(newState));
    }

    setState(newState) {
        if (this.currentState) {
            this.currentState.exit(this.context);
        }

        this.currentState = newState;

        if (this.currentState) {
            this.currentState.enter(this.context);
        }
    }
}

/**
 * Example placeholder states
 * Replace these with your actual implementations
 */

