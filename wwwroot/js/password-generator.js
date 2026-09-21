(function () {
  "use strict";

  const CHARSETS = {
    lower: "abcdefghijklmnopqrstuvwxyz",
    upper: "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
    digits: "0123456789",
    symbols: "!@#$%^&*()-_=+[]{};:,.?/",
  };
  const AMBIGUOUS = new Set(["O", "0", "o", "I", "l", "1", "|", "`", "'", '"']);

  /**
   * Uniform random integer in [0, maxExclusive) using the Web Crypto RNG.
   * Rejection sampling avoids modulo bias. Math.random() is NEVER used.
   */
  function secureRandomInt(maxExclusive) {
    if (!Number.isInteger(maxExclusive) || maxExclusive <= 0) {
      throw new Error("maxExclusive must be a positive integer");
    }
    const RANGE = 0x100000000; // 2^32
    const limit = RANGE - (RANGE % maxExclusive); // largest unbiased multiple
    const buf = new Uint32Array(1);
    let x;
    do {
      crypto.getRandomValues(buf);
      x = buf[0];
    } while (x >= limit);
    return x % maxExclusive;
  }

  function filterAmbiguous(set, strip) {
    if (!strip) return set;
    return [...set].filter((c) => !AMBIGUOUS.has(c)).join("");
  }

  function generatePassword(opts) {
    const sets = [];
    if (opts.lower)
      sets.push(filterAmbiguous(CHARSETS.lower, opts.noAmbiguous));
    if (opts.upper)
      sets.push(filterAmbiguous(CHARSETS.upper, opts.noAmbiguous));
    if (opts.digits)
      sets.push(filterAmbiguous(CHARSETS.digits, opts.noAmbiguous));
    if (opts.symbols)
      sets.push(filterAmbiguous(CHARSETS.symbols, opts.noAmbiguous));

    const usable = sets.filter((s) => s.length > 0);
    if (usable.length === 0) return { password: "", entropy: 0 };

    const pool = usable.join("");
    const chars = [];

    // Guarantee at least one character from each selected class.
    for (const s of usable) chars.push(s.charAt(secureRandomInt(s.length)));

    while (chars.length < opts.length) {
      chars.push(pool.charAt(secureRandomInt(pool.length)));
    }

    // Fisher–Yates shuffle with the crypto RNG.
    for (let i = chars.length - 1; i > 0; i--) {
      const j = secureRandomInt(i + 1);
      const tmp = chars[i];
      chars[i] = chars[j];
      chars[j] = tmp;
    }

    const password = chars.slice(0, opts.length).join("");
    const entropy = opts.length * Math.log2(pool.length);
    return { password, entropy };
  }

  function labelFor(entropy) {
    if (entropy < 40) return { text: "Weak", cls: "bg-danger", pct: 25 };
    if (entropy < 60) return { text: "Fair", cls: "bg-warning", pct: 50 };
    if (entropy < 80) return { text: "Good", cls: "bg-info", pct: 75 };
    return { text: "Strong", cls: "bg-success", pct: 100 };
  }

  function init() {
    const root = document.getElementById("pwgen");
    if (!root) return;

    const output = document.getElementById("pwgen-output");
    const lengthInput = document.getElementById("pwgen-length");
    const lengthLabel = document.getElementById("pwgen-length-value");
    const bar = document.getElementById("pwgen-strength-bar");
    const barLabel = document.getElementById("pwgen-strength-label");
    const useBtn = document.getElementById("pwgen-use");
    const genBtn = document.getElementById("pwgen-generate");

    const boxes = {
      lower: document.getElementById("pwgen-lower"),
      upper: document.getElementById("pwgen-upper"),
      digits: document.getElementById("pwgen-digits"),
      symbols: document.getElementById("pwgen-symbols"),
      noAmbiguous: document.getElementById("pwgen-no-ambiguous"),
    };

    function currentOptions() {
      return {
        length: parseInt(lengthInput.value, 10) || 16,
        lower: boxes.lower.checked,
        upper: boxes.upper.checked,
        digits: boxes.digits.checked,
        symbols: boxes.symbols.checked,
        noAmbiguous: boxes.noAmbiguous.checked,
      };
    }

    function refresh() {
      const { password, entropy } = generatePassword(currentOptions());
      output.value = password;

      const info = labelFor(entropy);
      bar.className = "progress-bar " + info.cls;
      bar.style.width = info.pct + "%";
      barLabel.textContent =
        password.length === 0
          ? "Select at least one character class"
          : `${info.text} — ~${entropy.toFixed(0)} bits of entropy`;
    }

    lengthInput.addEventListener("input", () => {
      lengthLabel.textContent = lengthInput.value;
      refresh();
    });
    Object.values(boxes).forEach((b) => b.addEventListener("change", refresh));
    genBtn.addEventListener("click", refresh);

    useBtn.addEventListener("click", () => {
      const secret = document.getElementById("Secret");
      if (!secret || !output.value) return;
      secret.value = output.value;
      secret.dispatchEvent(new Event("input", { bubbles: true }));
    });

    lengthLabel.textContent = lengthInput.value;
    refresh();
  }

  document.addEventListener("DOMContentLoaded", init);
})();
