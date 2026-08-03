// web/recruitment/src/features/template/__tests__/tokens.test.ts
//
// Guards the rule in brief 6.0: the preview and the .docx renderer must read
// every colour and measurement from tokens.ts. A hex code appearing in one
// renderer and again in the other is a code review finding — this test catches
// it before the reviewer does.
//
// The failure this prevents is not "wrong colour". It is "right in one file,
// stale in the other", which nobody notices until the two outputs disagree.

import { readFileSync, readdirSync, statSync } from "fs";
import { join } from "path";

const ROOT = join(__dirname, "..", "..");
const SEARCH_DIRS = ["preview", "docx", "template"];
const TOKENS_FILE = join(ROOT, "template", "tokens.ts");
const HEX = /#[0-9A-Fa-f]{6}\b/g;

function walk(dir: string): string[] {
  let out: string[] = [];
  let entries: string[];
  try {
    entries = readdirSync(dir);
  } catch {
    return out; // directory may not exist yet (e.g. docx before Stage 5)
  }
  for (const entry of entries) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      if (entry === "__tests__") continue; // tests may name colours to assert on them
      out = out.concat(walk(full));
    } else if (/\.tsx?$/.test(full) && !/\.test\.tsx?$/.test(full)) {
      out.push(full);
    }
  }
  return out;
}

describe("template tokens are the single source of truth", () => {
  it("has no hex colour literal outside tokens.ts", () => {
    const offenders: string[] = [];

    for (const dir of SEARCH_DIRS) {
      for (const file of walk(join(ROOT, dir))) {
        if (file === TOKENS_FILE) continue;
        const matches = readFileSync(file, "utf8").match(HEX);
        if (matches) offenders.push(`${file}: ${matches.join(", ")}`);
      }
    }

    expect(offenders).toEqual([]);
  });

  it("tokens.ts carries the template colours from brief 6.2 and 6.3", () => {
    const src = readFileSync(TOKENS_FILE, "utf8");
    for (const hex of ["#1E1560", "#EEF0FF", "#E8600A", "#0F0E1A"]) {
      expect(src).toContain(hex);
    }
  });
});
