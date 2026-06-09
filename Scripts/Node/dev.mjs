import { runCommand, runCommandAsync } from "./Helpers/RunCommand.mjs";
import { clientFolderName, serverFolderName } from "./Helpers/Globals.mjs";
import open from "open";

const shouldOpenBrowser = !process.argv.includes("--no");

runCommand("docker compose up mariadb phpmyadmin --build -d");
runCommand("dotnet ef database update", serverFolderName);
runCommandAsync("dotnet watch run", serverFolderName);

setTimeout(() => {
    runCommandAsync("npm start", clientFolderName);
}, 5000);

if (shouldOpenBrowser) {
    setTimeout(() => {
        open("https://localhost:4200");
        open("http://localhost:5247/swagger");
    }, 7500);
}
