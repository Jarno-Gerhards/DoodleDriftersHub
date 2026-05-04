import State from "../State.js";

export default class TestState extends State {
    enter(context) {
        console.log("Enter Test");
    }

    update(context) {}

    exit(context) {
        console.log("Exit Test");
    }
}