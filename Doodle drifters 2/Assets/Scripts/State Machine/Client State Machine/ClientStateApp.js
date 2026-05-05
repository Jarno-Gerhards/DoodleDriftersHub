import StateMachine from "./StateMachine.js";
import { attachDomRouter, attachPageRouter } from "./UI/ClientUIRouter.js";
import { defaultPageRoutes } from "./UI/DefaultPageRoutes.js";

export function startClientStateApp(options = {}) {
    const stateMachine = options.stateMachine || new StateMachine();
    const mode = options.mode || "page";

    if (mode === "dom") {
        attachDomRouter(stateMachine, options.root || document);
    } else {
        attachPageRouter(stateMachine, options.routes || defaultPageRoutes, {
            force: options.force,
            replace: options.replace
        });
    }

    stateMachine.start();

    return stateMachine;
}
