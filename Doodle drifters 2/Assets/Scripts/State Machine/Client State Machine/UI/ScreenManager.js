export default class ScreenManager {
    constructor() {
        this.screens = new Map();
        this.active = null;
    }

    register(name, screen) {
        if (!name || !screen) {
            return;
        }

        this.screens.set(name, screen);
    }

    show(name, payload) {
        this.screens.forEach((screen, key) => {
            if (key === name) {
                if (screen.enter) {
                    screen.enter(payload);
                }
            } else if (screen.exit) {
                screen.exit();
            }
        });

        this.active = name;
    }

    hideAll() {
        this.screens.forEach((screen) => {
            if (screen.exit) {
                screen.exit();
            }
        });

        this.active = null;
    }
}
