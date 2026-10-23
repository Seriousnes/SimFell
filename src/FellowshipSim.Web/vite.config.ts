import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// FellowshipSim.Service's address: Aspire passes it as SERVICE_HTTP; `dotnet run` of FellowshipSim.Service alone listens on port 5000.
const service = process.env.SERVICE_HTTP ?? "http://localhost:5000";

export default defineConfig({
  plugins: [react()],
  // Component styles (X.module.scss) load src/_design-system.scss with @use "design-system" as design.
  css: { preprocessorOptions: { scss: { loadPaths: ["./src"] } } },
  server: {
    // The review pages link back to "/", so they are served from the same address as this page.
    proxy: {
      "/api": { target: service, changeOrigin: true },
      "/reviews": { target: service, changeOrigin: true },
    },
  },
});
