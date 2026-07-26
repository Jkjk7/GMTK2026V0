import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mkdtemp, mkdir, rm } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import test from "node:test";
const require = createRequire(import.meta.url);
const sharp = require("sharp");

import { processSheet, processSingle } from "./process-countdown-assets.mjs";

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

test("sheet entry rejects a name count that does not match columns times rows", async () => {
  await withTempRoot(async (root) => {
    await assert.rejects(
      () => processSheet({ columns: 2, rows: 2, names: ["a"] }, root),
      /requires exactly 4 names/
    );
  });
});
