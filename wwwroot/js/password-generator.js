/*
 * CipherVault — Password generator.
 * Exposes window.CipherVaultGenerator.generate(options).
 * Uses window.crypto.getRandomValues (NOT Math.random).
 */
(function () {
  "use strict";

  console.log("[password-generator] loaded");

  const CHARSETS = {
    lower: "abcdefghijkmnopqrstuvwxyz",
    upper: "ABCDEFGHJKLMNPQRSTUVWXYZ",
    digits: "23456789",
    symbols: "!@#$%^&*()-_=+[]{};:,.<>?/~",
  };

  // Uniform random integer in [0, maxExclusive) using crypto RNG.
  // Rejection sampling avoids modulo bias.
  function randomInt(maxExclusive) {
    if (!Number.isInteger(maxExclusive) || maxExclusive <= 0) {
      throw new Error("maxExclusive must be a positive integer");
    }
    const RANGE = 0x100000000; // 2^32
    const limit = RANGE - (RANGE % maxExclusive);
    const buf = new Uint32Array(1);
    let x;
    do {
      window.crypto.getRandomValues(buf);
      x = buf[0];
    } while (x >= limit);
    return x % maxExclusive;
  }

  function generate(opts) {
    const o = opts || {};
    const length = Math.max(8, Math.min(128, o.length || 20));

    const sets = [];
    if (o.lower !== false) sets.push(CHARSETS.lower);
    if (o.upper !== false) sets.push(CHARSETS.upper);
    if (o.digits !== false) sets.push(CHARSETS.digits);
    if (o.symbols !== false) sets.push(CHARSETS.symbols);

    if (sets.length === 0) return "";

    const pool = sets.join("");

    // At least one character from each selected set.
    const chars = [];
    for (const set of sets) {
      if (chars.length < length) {
        chars.push(set.charAt(randomInt(set.length)));
      }
    }

    while (chars.length < length) {
      chars.push(pool.charAt(randomInt(pool.length)));
    }

    // Fisher–Yates shuffle with crypto RNG.
    for (let i = chars.length - 1; i > 0; i--) {
      const j = randomInt(i + 1);
      const tmp = chars[i];
      chars[i] = chars[j];
      chars[j] = tmp;
    }

    return chars.slice(0, length).join("");
  }

  window.CipherVaultGenerator = { generate: generate };
  console.log("[password-generator] CipherVaultGenerator exposed");
})();
