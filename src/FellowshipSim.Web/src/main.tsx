import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
// The element defaults load before the components' styles.
import "./app.css";
import { App } from "./App";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
