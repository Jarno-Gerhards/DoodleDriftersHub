import StateMachine, { GameStateType } from "./StateMachine.js";
import { attachDomRouter, attachPageRouter } from "./UI/ClientUIRouter.js";
import { defaultPageRoutes } from "./UI/DefaultPageRoutes.js";

const DEFAULT_STORAGE_KEY = "clientState";

export function startClientStateApp(options = {}) {
    const stateMachine = options.stateMachine || new StateMachine();
    const mode = options.mode || "page";
    const storageKey = options.storageKey || DEFAULT_STORAGE_KEY;
    const persistState = options.persistState !== false;

    if (mode === "dom") {
        attachDomRouter(stateMachine, options.root || document);
    } else {
        attachPageRouter(stateMachine, options.routes || defaultPageRoutes, {
            force: options.force,
            replace: options.replace,
            deviceKind: options.deviceKind,
            getDeviceKind: options.getDeviceKind
        });
    }

    if (persistState) {
        registerStatePersistence(stateMachine, storageKey);
    }

    const initialState = resolveInitialState(options, storageKey);
    if (initialState) {
        stateMachine.switchState(initialState);
    } else {
        stateMachine.start();
    }

    return stateMachine;
}

function resolveInitialState(options, storageKey) {
    if (typeof options.getInitialState === "function") {
        const resolved = options.getInitialState();
        return isValidState(resolved) ? resolved : null;
    }

    if (isValidState(options.initialState)) {
        return options.initialState;
    }

    if (options.persistState === false) {
        return null;
    }

    const stored = readStoredState(storageKey);
    return isValidState(stored) ? stored : null;
}

function registerStatePersistence(stateMachine, storageKey) {
    if (!hasStorage()) {
        return;
    }

    stateMachine.onStateChanged(({ nextState }) => {
        if (nextState) {
            sessionStorage.setItem(storageKey, nextState);
        }
    });
}

function readStoredState(storageKey) {
    if (!hasStorage()) {
        return null;
    }

    return sessionStorage.getItem(storageKey);
}

function hasStorage() {
    return typeof sessionStorage !== "undefined";
}

function isValidState(value) {
    return Object.values(GameStateType).includes(value);
}
