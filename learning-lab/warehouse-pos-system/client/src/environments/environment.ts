// Single environment for now — Angular's usual dev/prod environment.ts
// split doesn't help much until there's an actual second environment to
// point at. It also has a real limitation worth knowing for later: values
// here are baked in at BUILD time, which fights a Docker "build once,
// deploy anywhere" workflow. When F4 wires up docker-compose, the more
// container-friendly move is to fetch a small config.json from the served
// app at *runtime* instead — worth revisiting then rather than guessing now.
export const environment = {
  apiBaseUrl: 'http://localhost:5058',
};
