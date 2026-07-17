// Static server for the published preview.
//
// Blazor loads the .NET runtime via WebAssembly.instantiateStreaming(), which requires the .wasm to
// be served as Content-Type: application/wasm. Most default static servers don't, which fails the
// load. Hence the explicit TYPES table below.
//
//     node serve.js ./out/wwwroot 5080

const http = require("http");
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(process.argv[2] || "./out/wwwroot");
const PORT = parseInt(process.argv[3] || "5080", 10);

const TYPES = {
  ".html": "text/html",
  ".js": "text/javascript",
  ".css": "text/css",
  ".wasm": "application/wasm",
  ".json": "application/json",
  ".ico": "image/x-icon",
};

http
  .createServer((req, res) => {
    let url = decodeURIComponent(req.url.split("?")[0]);
    if (url === "/") url = "/index.html";

    const file = path.join(ROOT, url);

    if (!file.startsWith(ROOT) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
      res.writeHead(404);
      return res.end("not found");
    }

    res.writeHead(200, { "Content-Type": TYPES[path.extname(file)] || "application/octet-stream" });
    fs.createReadStream(file).pipe(res);
  })
  .listen(PORT, () => console.log(`Virtual Room preview → http://localhost:${PORT}`));
