const { env } = require("process");

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
  ? env.ASPNETCORE_URLS.split(";")[0]
  : "http://localhost:5247";

console.log(`Using proxy target: ${target}`);

const PROXY_CONFIG = [
  {
    context: ["/MolenImages", "/MolenAddedImages", "/api"],
    target: target,
    secure: false,
  },
];

module.exports = PROXY_CONFIG;
