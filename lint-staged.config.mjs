import path from "node:path";

const quoteForShell = (value) => `"${value.replaceAll('"', '\\"')}"`;

export default {
  "*.{js,cjs,mjs,jsx,ts,mts,cts,tsx,json,md,yml,yaml,css,scss}":
    "prettier --write",
  "*.cs": (files) => {
    const relativeFiles = files.map((file) =>
      path.relative(process.cwd(), file),
    );

    return `dotnet format whitespace PersonalUltra.sln --no-restore --include ${relativeFiles
      .map(quoteForShell)
      .join(" ")}`;
  },
};
