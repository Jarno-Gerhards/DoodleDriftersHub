Client UI routing

- Dom mode: add data-screen="Lobby" etc to sections in a single HTML page.
- Page mode: use a route map so state changes navigate to different HTML files.

Example (dom mode):

import { startClientStateApp } from "../ClientStateApp.js";

startClientStateApp({
    mode: "dom",
    root: document
});

Example (page mode):

import { startClientStateApp } from "../ClientStateApp.js";

startClientStateApp({
    mode: "page"
});
