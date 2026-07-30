import { runCommand } from "../Helpers/RunCommand.mjs";
import { isWindows } from "./Globals.mjs";

export function safeRun(cmd, folder = process.cwd()) {
  try {
    runCommand(cmd, folder);
  } catch (err) {
    if (isWindows && err.status === 128) {
      console.log(`Process not found, skipping: ${cmd}`);
    } else {
      throw err;
    }
  }
}
