import State from "../State.js";

export default class SolutionState extends State {
    enter(context) {
        console.log("Enter Solution");
    }

    update(context) {}

    exit(context) {
        console.log("Exit Solution");
    }
}
