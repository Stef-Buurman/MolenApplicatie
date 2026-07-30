import readline from "readline";
import { safeRun } from "./Helpers/SafeRun.mjs";
import { serverFolderName } from "./Helpers/Globals.mjs";

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
});

rl.question("Enter the migration name: ", (migrationName) => {
  rl.close();
  console.log();
  safeRun(
    `dotnet ef migrations add ${migrationName} && dotnet ef database update`,
    serverFolderName,
  );
});
