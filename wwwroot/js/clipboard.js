/*
 * CipherVault — Copy-to-clipboard helper.
 *
 * Security behaviour:
 *   - Works only in a secure context (HTTPS). navigator.clipboard is undefined otherwise.
 *   - After a successful copy, the OS clipboard is overwritten with an empty
 *     string after CLEAR_AFTER_MS milliseconds.
 *   - Copy button is enabled ONLY when its target has [data-revealed="true"]
 *     AND the target's value/textContent is non-empty. Placeholder text can
 *     never be copied.
 *   - No secret is kept in JS state longer than the clipboard write call.
 */
(function () {
  "use strict";

  const CLEAR_AFTER_MS = 10000; // 10 seconds — master prompt requirement
  const clearTimers = new WeakMap();

  // ---- Feature detection ----
  const clipboardAvailable =
    window.isSecureContext &&
    navigator.clipboard &&
    typeof navigator.clipboard.writeText === "function";

  // ---- Toast ----
  function showToast(message, isError) {
    const existing = document.getElementById("cv-toast");
    if (existing) existing.remove();

    const toast = document.createElement("div");
    toast.id = "cv-toast";
    toast.textContent = message;
    toast.style.cssText = [
      "position:fixed",
      "bottom:24px",
      "right:24px",
      "background:" + (isError ? "#f85149" : "#3fb950"),
      "color:#fff",
      "padding:10px 16px",
      "border-radius:6px",
      "font-size:0.9rem",
      "box-shadow:0 4px 12px rgba(0,0,0,0.35)",
      "z-index:9999",
      "opacity:0",
      "transition:opacity 0.2s ease",
      "pointer-events:none",
    ].join(";");

    document.body.appendChild(toast);
    requestAnimationFrame(() => {
      toast.style.opacity = "1";
    });

    setTimeout(() => {
      toast.style.opacity = "0";
      setTimeout(() => toast.remove(), 250);
    }, 2200);
  }

  // ---- Clipboard ops ----
  async function writeClipboard(text) {
    if (!clipboardAvailable) return false;
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      return false;
    }
  }

  async function clearClipboard() {
    if (!clipboardAvailable) return;
    try {
      await navigator.clipboard.writeText("");
    } catch {
      /* best-effort */
    }
  }

  function scheduleClear(btn) {
    // Cancel any previous timer for this button.
    const previous = clearTimers.get(btn);
    if (previous) {
      if (previous.timer) clearTimeout(previous.timer);
      if (previous.cleanup) previous.cleanup();
    }

    const state = {
      timer: null,
      attempts: 0,
      maxAttempts: 120, // ~2 minutes of retries at 1s interval
      cleanup: null,
    };
    clearTimers.set(btn, state);

    let retryInterval = null;
    let onFocusHandler = null;

    const stopRetrying = () => {
      if (retryInterval) {
        clearInterval(retryInterval);
        retryInterval = null;
      }
      if (onFocusHandler) {
        window.removeEventListener("focus", onFocusHandler);
        document.removeEventListener("visibilitychange", onFocusHandler);
        onFocusHandler = null;
      }
    };

    state.cleanup = stopRetrying;

    const tryClear = async () => {
      state.attempts++;
      if (!clipboardAvailable) return true; // nothing to do
      try {
        await navigator.clipboard.writeText("");
        console.debug(
          "[clipboard] cleared after",
          state.attempts,
          "attempt(s)",
        );
        return true;
      } catch (err) {
        console.debug(
          "[clipboard] clear attempt failed:",
          err && err.name,
          err && err.message,
        );
        return false;
      }
    };

    // First attempt after 10s.
    state.timer = setTimeout(async () => {
      const ok = await tryClear();
      if (ok) {
        stopRetrying();
        showToast("Clipboard cleared");
        return;
      }

      // Focus ne aate hi ek aur try karo.
      onFocusHandler = async () => {
        if (document.hasFocus() || document.visibilityState === "visible") {
          const cleared = await tryClear();
          if (cleared) {
            stopRetrying();
            showToast("Clipboard cleared");
          }
        }
      };
      window.addEventListener("focus", onFocusHandler);
      document.addEventListener("visibilitychange", onFocusHandler);

      // Background retries every 1s (best-effort).
      retryInterval = setInterval(async () => {
        if (state.attempts >= state.maxAttempts) {
          stopRetrying();
          return;
        }
        const cleared = await tryClear();
        if (cleared) {
          stopRetrying();
          showToast("Clipboard cleared");
        }
      }, 1000);

      // Give the user an immediate heads-up.
      showToast("Clipboard will clear when you return to this tab", true);
    }, CLEAR_AFTER_MS);
  }

  // ---- Target helpers ----
  function getText(el) {
    if (!el) return "";
    // input / textarea keep their value in .value, not textContent
    if (typeof el.value === "string") return el.value.trim();
    return (el.textContent || "").trim();
  }

  function isRevealed(el) {
    return (
      el &&
      el.getAttribute("data-revealed") === "true" &&
      getText(el).length > 0
    );
  }

  // ---- Button sync ----
  function syncButton(btn, target) {
    if (!clipboardAvailable) {
      btn.disabled = true;
      btn.title = "Clipboard not available (HTTPS required)";
      return;
    }
    btn.disabled = !isRevealed(target);
    btn.title = btn.disabled
      ? "Reveal first to enable copy"
      : "Copy (clipboard clears in 10s)";
  }

  // ---- Wire one copy button ----
  function wireCopyButton(btn) {
    const selector = btn.getAttribute("data-copy-target");
    if (!selector) return;

    const target = document.querySelector(selector);
    if (!target) return;

    // Initial state
    syncButton(btn, target);

    // Watch for changes to the target's value and data-revealed attribute
    const observer = new MutationObserver(() => syncButton(btn, target));
    observer.observe(target, {
      attributes: true,
      attributeFilter: ["data-revealed"],
      childList: true,
      characterData: true,
      subtree: true,
    });

    // input/textarea changes don't fire MutationObserver, so listen to input event
    if (
      target instanceof HTMLInputElement ||
      target instanceof HTMLTextAreaElement
    ) {
      target.addEventListener("input", () => syncButton(btn, target));
      // Also poll once after a tick, in case JS reveal handler set .value directly
      setInterval(() => syncButton(btn, target), 500);
    }

    btn.addEventListener("click", async () => {
      if (btn.disabled) return;
      const text = getText(target);
      if (!text) return;

      const ok = await writeClipboard(text);
      if (ok) {
        showToast("Copied — clipboard will clear in 10 seconds");
        scheduleClear(btn);
      } else {
        showToast("Copy failed", true);
      }
    });
  }

  // ---- Init ----
  document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-copy-target]").forEach(wireCopyButton);
  });
})();
