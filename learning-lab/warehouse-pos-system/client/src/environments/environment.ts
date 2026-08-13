// Single environment for now — Angular's usual dev/prod environment.ts
// split doesn't help much until there's an actual second environment to
// point at. It also has a real limitation worth knowing for later: values
// here are baked in at BUILD time, which fights a Docker "build once,
// deploy anywhere" workflow. When F4 wires up docker-compose, the more
// container-friendly move is to fetch a small config.json from the served
// app at *runtime* instead — worth revisiting then rather than guessing now.
export const environment = {
  apiBaseUrl: 'http://localhost:5058',
  // Notifications.API's OWN port, not the gateway's. Every other feature
  // in this app talks to the gateway (5058) — this is the one exception,
  // and deliberately so: the SignalR hub is a live WebSocket connection,
  // and Ocelot's HTTP-forwarding model doesn't proxy the upgrade
  // handshake reliably (see the README's E1 section for the full
  // reasoning). NotificationFeedService's plain HTTP calls (GetRecent,
  // mark-as-read) still go through apiBaseUrl like everything else —
  // only the hub connection itself uses this.
  notificationsHubUrl: 'http://localhost:5298',
};
