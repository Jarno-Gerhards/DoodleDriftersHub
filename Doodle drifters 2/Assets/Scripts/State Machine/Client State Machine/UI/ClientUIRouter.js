import DomScreen from "./DomScreen.js";
import ScreenManager from "./ScreenManager.js";

const DEVICE_PHONE = "phone";
const DEVICE_DESKTOP = "desktop";

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
    const handler = ({ nextState, context }) => {
        navigateToState(nextState, routes, options, context);
    };

    stateMachine.onStateChanged(handler);

    if (stateMachine.getCurrentStateType) {
        const current = stateMachine.getCurrentStateType();
        if (current) {
            const context = stateMachine.context || null;
            navigateToState(current, routes, options, context);
        }
    }

    return {
        detach: () => stateMachine.offStateChanged(handler)
    };
}

function navigateToState(state, routes, options, context) {
    const deviceKind = resolveDeviceKind(options);
    const targetUrl = resolveRoute(routes, state, deviceKind, context);
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

function resolveRoute(routes, state, deviceKind, context) {
    if (!routes) {
        return null;
    }

    if (typeof routes === "function") {
        return routes({ state, deviceKind, context }) || null;
    }

    if (routes instanceof Map) {
        return routes.get(state) || null;
    }

    if (typeof routes === "object") {
        if (routes.desktop || routes.phone) {
            const fallback = routes.desktop || routes.phone;
            const map = routes[deviceKind] || fallback;
            if (map instanceof Map) {
                return map.get(state) || null;
            }

            if (typeof map === "object") {
                return map[state] || null;
            }

            return null;
        }

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

function resolveDeviceKind(options) {
    if (options.deviceKind) {
        return options.deviceKind;
    }

    if (typeof options.getDeviceKind === "function") {
        return options.getDeviceKind();
    }

    return getDefaultDeviceKind();
}

function getDefaultDeviceKind() {
    if (typeof window === "undefined") {
        return DEVICE_DESKTOP;
    }

    const hasMatchMedia = typeof window.matchMedia === "function";
    const coarsePointer = hasMatchMedia && window.matchMedia("(pointer: coarse)").matches;
    const narrowScreen = hasMatchMedia && window.matchMedia("(max-width: 900px)").matches;
    const touchPoints = typeof navigator !== "undefined" && navigator.maxTouchPoints > 0;

    return coarsePointer || narrowScreen || touchPoints ? DEVICE_PHONE : DEVICE_DESKTOP;
}
