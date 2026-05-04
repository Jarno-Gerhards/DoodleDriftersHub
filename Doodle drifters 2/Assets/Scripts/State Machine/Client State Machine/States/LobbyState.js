import State from "../State.js";

export default class LobbyState extends State {
    enter(context) {
        console.log("Enter Lobby");
    }

    update(context) {}

    exit(context) {
        console.log("Exit Lobby");
    }
}