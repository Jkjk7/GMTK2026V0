import { access, mkdir, readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const sharp = require("sharp");

const transparent = { r: 0, g: 0, b: 0, alpha: 0 };

export function keyedAlpha(r, g, b, originalAlpha) {
  const distance = Math.hypot(r, g - 255, b);
  if (distance <= 42) return 0;
  if (distance >= 112) return originalAlpha;
  return Math.round(originalAlpha * ((distance - 42) / 70));
}

function resolveEntryPath(rootDir, entryPath) {
  return path.isAbsolute(entryPath) ? entryPath : path.resolve(rootDir, entryPath);
}

async function sourceExists(source) {
  try {
    await access(source);
    return true;
  } catch {
    return false;
  }
}

async function requireSource(entry, rootDir) {
  const source = resolveEntryPath(rootDir, entry.source);
  if (await sourceExists(source)) return source;
  if (entry.allowMissing) {
    console.log(`SKIP ${entry.source}`);
    return null;
  }
  throw new Error(`Missing source image: ${source}`);
}

async function processTransparentBuffer(buffer, output, size = 512) {
  const { data, info } = await sharp(buffer).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let offset = 0; offset < data.length; offset += info.channels) {
    data[offset + 3] = keyedAlpha(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);
  }

  await mkdir(path.dirname(output), { recursive: true });
  await sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } })
    .png()
    .trim({ background: transparent })
    .resize(size, size, { fit: "contain", background: transparent })
    .png()
    .toFile(output);
}

export async function processSingle(entry, rootDir) {
  const source = await requireSource(entry, rootDir);
  if (!source) return;
  const output = resolveEntryPath(rootDir, entry.output);
  await processTransparentBuffer(await readFile(source), output, entry.size ?? 512);
}

function validateGrid(entry) {
  if (!Number.isInteger(entry.columns) || entry.columns <= 0 || !Number.isInteger(entry.rows) || entry.rows <= 0) {
    throw new Error("Sheet entries require positive integer columns and rows");
  }
  const expectedNames = entry.columns * entry.rows;
  if (!Array.isArray(entry.names) || entry.names.length !== expectedNames) {
    throw new Error(`Sheet entry requires exactly ${expectedNames} names`);
  }
}

export async function processSheet(entry, rootDir) {
  validateGrid(entry);
  const sourcePath = await requireSource(entry, rootDir);
  if (!sourcePath) return;

  const source = sharp(sourcePath);
  const metadata = await source.metadata();
  const width = metadata.width - (metadata.width % entry.columns);
  const height = metadata.height - (metadata.height % entry.rows);
  if (width === 0 || height === 0) {
    throw new Error(`Source image is too small for ${entry.columns} by ${entry.rows} grid`);
  }
  const resized = source.resize(width, height, { fit: "fill" });
  const cellWidth = width / entry.columns;
  const cellHeight = height / entry.rows;

  for (let row = 0; row < entry.rows; row += 1) {
    for (let col = 0; col < entry.columns; col += 1) {
      const index = row * entry.columns + col;
      const region = {
        left: col * cellWidth,
        top: row * cellHeight,
        width: cellWidth,
        height: cellHeight
      };
      const output = resolveEntryPath(rootDir, entry.names[index]);
      await processTransparentBuffer(await resized.clone().extract(region).png().toBuffer(), output, entry.size ?? 512);
    }
  }
}

export async function processOpaque(entry, rootDir) {
  const source = await requireSource(entry, rootDir);
  if (!source) return;
  const output = resolveEntryPath(rootDir, entry.output);
  await mkdir(path.dirname(output), { recursive: true });
  await sharp(source)
    .resize(entry.width ?? 1536, entry.height ?? 512, { fit: "cover", position: "centre" })
    .png()
    .toFile(output);
}

export async function processManifest(manifestPath, repositoryRoot) {
  const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
  const rootDir = resolveEntryPath(repositoryRoot, manifest.root);
  for (const entry of manifest.entries) {
    if (entry.mode === "sheet") await processSheet(entry, rootDir);
    else if (entry.mode === "opaque") await processOpaque(entry, rootDir);
    else await processSingle(entry, rootDir);
  }
}

async function main() {
  const manifestIndex = process.argv.indexOf("--manifest");
  if (manifestIndex === -1 || !process.argv[manifestIndex + 1]) {
    throw new Error("Usage: node Tools/Art/process-countdown-assets.mjs --manifest <path>");
  }
  const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
  const repositoryRoot = path.resolve(scriptDirectory, "../..");
  await processManifest(resolveEntryPath(repositoryRoot, process.argv[manifestIndex + 1]), repositoryRoot);
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  main().catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  });
}
