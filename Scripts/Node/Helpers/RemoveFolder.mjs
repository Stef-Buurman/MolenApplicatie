import { rm } from "fs/promises";
import { existsSync } from "fs";

export async function remove(path) {
    if (existsSync(path)) {
        console.log(`🗑️ Removing ${path}`);
        await rm(path, { recursive: true, force: true });
    }
}
