import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mkdtemp, mkdir, readFile, rm } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
const require = createRequire(import.meta.url);
const sharp = require("sharp");

import { keyedPixel, processOpaque, processSheet, processSingle } from "./process-countdown-assets.mjs";

async function withTempRoot(run) {
  const root = await mkdtemp(path.join(os.tmpdir(), "countdown-assets-"));
  try {
    await run(root);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
}

async function writeGreenAndCyanFixture(file) {
  const pixels = Buffer.alloc(512 * 512 * 4, 0);
  for (let index = 0; index < pixels.length; index += 4) {
    pixels[index + 1] = 255;
    pixels[index + 3] = 255;
  }

  for (const pixel of [1, 512, (512 * 511) + 511]) {
    const offset = pixel * 4;
    pixels[offset] = 0;
    pixels[offset + 1] = 255;
    pixels[offset + 2] = 255;
  }

  await sharp(pixels, { raw: { width: 512, height: 512, channels: 4 } })
    .png()
    .toFile(file);
}

test("green key becomes transparent without erasing a cyan subject", async () => {
  await withTempRoot(async (root) => {
    await writeGreenAndCyanFixture(path.join(root, "source.png"));
    const output = path.join(root, "output.png");

    await processSingle({ source: "source.png", output: "output.png", size: 512 }, root);

    const pixels = await sharp(output).ensureAlpha().raw().toBuffer();
    assert.equal(pixels[3], 0);              // keyed corner
    assert.ok(pixels[7] > 240);              // cyan subject alpha
  });
});

test("strong green antialias pixels are fully removed", () => {
  const pixel = keyedPixel(45, 220, 40, 255);
  assert.deepEqual(pixel, { r: 0, g: 0, b: 0, alpha: 0 });
});

test("bright yellow-green edge spill is fully removed", () => {
  assert.equal(keyedPixel(96, 255, 16, 255).alpha, 0);
});

test("single sprites are padded to exactly 512 square pixels", async () => {
  await withTempRoot(async (root) => {
    await mkdir(path.join(root, "source"));
    await sharp({ create: { width: 200, height: 100, channels: 4, background: "#00FFFF" } })
      .png()
      .toFile(path.join(root, "source", "sprite.png"));
    const output = path.join(root, "output.png");

    await processSingle({ source: "source/sprite.png", output: "output.png", size: 512 }, root);

    const info = await sharp(output).metadata();
    assert.deepEqual([info.width, info.height], [512, 512]);
  });
});

test("opaque backdrops flatten transparent source pixels", async () => {
  await withTempRoot(async (root) => {
    await sharp({ create: { width: 2, height: 2, channels: 4, background: { r: 255, g: 0, b: 0, alpha: 0 } } })
      .png()
      .toFile(path.join(root, "backdrop.png"));
    const output = path.join(root, "output.png");

    await processOpaque({ source: "backdrop.png", output: "output.png", width: 4, height: 2 }, root);

    const pixels = await sharp(output).ensureAlpha().raw().toBuffer();
    for (let offset = 3; offset < pixels.length; offset += 4) assert.equal(pixels[offset], 255);
  });
});

test("sheet entry rejects a name count that does not match columns times rows", async () => {
  await withTempRoot(async (root) => {
    await assert.rejects(
      () => processSheet({ columns: 2, rows: 2, names: ["a"] }, root),
      /requires exactly 4 names/
    );
  });
});

test("manifest applies the corrected standalone hourglass after the UI sheet", async () => {
  const manifestPath = path.join(path.dirname(fileURLToPath(import.meta.url)), "countdown-assets.json");
  const manifest = JSON.parse(await readFile(manifestPath, "utf8"));
  const sheetIndex = manifest.entries.findIndex((entry) => entry.source === "Sources/environment_ui_sheet.png");
  const overrideIndex = manifest.entries.findIndex((entry) => entry.source === "Sources/hourglass_frame_v2.png");
  assert.ok(sheetIndex >= 0);
  assert.ok(overrideIndex > sheetIndex);
  assert.equal(manifest.entries[overrideIndex].output, "UI/hourglass_frame.png");
});
