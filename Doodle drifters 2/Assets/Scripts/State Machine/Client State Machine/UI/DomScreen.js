export default class DomScreen {
    constructor(element) {
        this.element = element;
    }

    enter() {
        if (this.element) {
            this.element.hidden = false;
        }
    }

    exit() {
        if (this.element) {
            this.element.hidden = true;
        }
    }
}
