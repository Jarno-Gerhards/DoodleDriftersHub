import DomScreen from "./DomScreen.js";
import ScreenManager from "./ScreenManager.js";

export function createDomScreenManager(root = document) {
    const manager = new ScreenManager();
    const nodes = root.querySelectorAll("[data-screen]");

    nodes.forEach((node) => {
        const name = node.dataset.screen;
        manager.register(name, new DomScreen(node));
    });

    return manager;
}

export function attachDomRouter(stateMachine, root = document) {
    const manager = createDomScreenManager(root);
    const handler = ({ nextState }) => {
        manager.show(nextState);
    };

    stateMachine.onStateChanged(handler);

    if (stateMachine.getCurrentStateType) {
        const current = stateMachine.getCurrentStateType();
        if (current) {
            manager.show(current);
        }
    }

    return {
        manager,
        detach: () => stateMachine.offStateChanged(handler)
    };
}

export function attachPageRouter(stateMachine, routes, options = {}) {
    const handler = ({ nextState }) => {
        navigateToState(nextState, routes, options);
    };

    stateMachine.onStateChanged(handler);

    if (stateMachine.getCurrentStateType) {
        const current = stateMachine.getCurrentStateType();
        if (current) {
            navigateToState(current, routes, options);
        }
    }

    return {
        detach: () => stateMachine.offStateChanged(handler)
    };
}

function navigateToState(state, routes, options) {
    const targetUrl = resolveRoute(routes, state);
    if (!targetUrl) {
        return;
    }

    if (!options.force && isSamePage(targetUrl)) {
        return;
    }

    if (options.replace) {
        window.location.replace(targetUrl);
    } else {
        window.location.href = targetUrl;
    }
}

function resolveRoute(routes, state) {
    if (!routes) {
        return null;
    }

    if (routes instanceof Map) {
        return routes.get(state) || null;
    }

    if (typeof routes === "object") {
        return routes[state] || null;
    }

    return null;
}

function isSamePage(targetUrl) {
    try {
        const target = new URL(targetUrl, window.location.href);
        const current = new URL(window.location.href);
        return target.href === current.href;
    } catch (error) {
        return false;
    }
}
