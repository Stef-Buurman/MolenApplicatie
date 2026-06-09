import { execSync } from "child_process";
import { spawn } from "child_process";
import fs from "fs";
import path from "path";

export function runCommand(cmd, folder = process.cwd()) {
    const cwd = folder ? path.resolve(process.cwd(), folder) : process.cwd();

    if (!fs.existsSync(cwd)) {
        throw new Error(`Folder does not exist: ${cwd}`);
    }
    execSync(cmd, { cwd, stdio: "inherit", shell: true });
}

export function runCommandAsync(cmd, folder = process.cwd()) {
    const cwd = folder ? path.resolve(process.cwd(), folder) : process.cwd();
    const child = spawn(cmd, { cwd, shell: true, stdio: "inherit" });
    return child;
}
