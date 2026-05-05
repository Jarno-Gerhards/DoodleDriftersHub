import { GameStateType } from "../StateMachine.js";

const assetsRoot = new URL("../../../../", import.meta.url);

const desktopRoutes = new Map([
    [GameStateType.LOBBY, new URL("Client/Join Room Environment/index.html", assetsRoot).toString()],
    [GameStateType.DRAWING, new URL("Scripts/DrawingClient/index.html", assetsRoot).toString()],
    [GameStateType.SOLUTION, new URL("Scripts/DrawingClient/solution.html", assetsRoot).toString()],
    [GameStateType.VOTING, new URL("Scripts/VotingClient/index.html", assetsRoot).toString()]
]);

const phoneRoutes = new Map([
    [GameStateType.LOBBY, new URL("Client/Join Room Environment/index.html", assetsRoot).toString()],
    [GameStateType.DRAWING, new URL("Scripts/DrawingClient/Phone/index.html", assetsRoot).toString()],
    [GameStateType.SOLUTION, new URL("Scripts/DrawingClient/Phone/solution.html", assetsRoot).toString()],
    [GameStateType.VOTING, new URL("Scripts/VotingClient/Phone/index.html", assetsRoot).toString()]
]);

export const defaultPageRoutes = {
    desktop: desktopRoutes,
    phone: phoneRoutes
};
