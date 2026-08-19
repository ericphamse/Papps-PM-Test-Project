// web/recruitment/jest.config.js
//
// Jest for Next.js. next/jest wires up the TypeScript transform, the `@/`
// path alias from tsconfig, and the env file loading, so tests import modules
// exactly the way the app does.
//
// testEnvironment is 'node' deliberately: the docx renderer and the token
// checks are pure Node work — no DOM needed. If a component test is added
// later that needs a DOM, give that file a jsdom docblock rather than
// switching the whole suite.

const nextJest = require("next/jest");

const createJestConfig = nextJest({ dir: "./" });

module.exports = createJestConfig({
  testEnvironment: "node",
  testMatch: ["**/__tests__/**/*.test.ts", "**/__tests__/**/*.test.tsx"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/src/$1",
  },
});