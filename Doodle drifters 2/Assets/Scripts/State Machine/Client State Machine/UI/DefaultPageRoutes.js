import { GameStateType } from "../StateMachine.js";

const assetsRoot = new URL("../../../../", import.meta.url);

export const defaultPageRoutes = new Map([
    [GameStateType.LOBBY, new URL("Client/Join Room Environment/index.html", assetsRoot).toString()],
    [GameStateType.DRAWING, new URL("Scripts/DrawingClient/index.html", assetsRoot).toString()],
    [GameStateType.VOTING, new URL("Scripts/VotingClient/index.html", assetsRoot).toString()]
]);
