import { safeRun } from "./Helpers/SafeRun.mjs";
import { isWindows } from "./Helpers/Globals.mjs";

safeRun("docker compose stop");

if (isWindows) {
  safeRun("taskkill /IM dotnet.exe /F");
  safeRun("taskkill /IM node.exe /F");
} else {
  safeRun("pkill -f 'dotnet watch run'");
  safeRun("pkill -f 'npm run dev'");
}
