// Preserve game-specific v2 fields beside the shared kit's normalized v1 sample.
// The kit still controls sample count, random selection, ready state and commands.
mergeInto(LibraryManager.library, {
  HYEOPGOK_PushBank: function (jsonPtr) {
    var incoming = JSON.parse(UTF8ToString(jsonPtr));
    var byId = Object.create(null);
    (incoming.items || []).forEach(function (item) { byId[item.id] = item; });
    window.__HYEOPGOK_SAMPLE_BANK__ = byId;
    var test = window.__GAME_TEST__;
    if (!test || typeof test.sampleProblems !== 'function' || test.__hyeopgokBankWrapped) return;
    var sampleBase = test.sampleProblems;
    test.sampleProblems = function (n) {
      return sampleBase.call(test, n).map(function (base) {
        // Base supplies the sampled ID and preserves the shared kit's random,
        // non-replacement selection. Return the corresponding source item itself:
        // v3.2's test contract requires the pack object without normalized fields
        // overwriting answerNumeric or dropping heterogeneous params.
        var source = window.__HYEOPGOK_SAMPLE_BANK__[base.id];
        return JSON.parse(JSON.stringify(source || base));
      });
    };
    test.__hyeopgokBankWrapped = true;
  }
});

// Endless-run record is intentionally game-local. Browsers with blocked storage
// still play normally because both operations are guarded.
mergeInto(LibraryManager.library, {
  HYEOPGOK_LoadBest: function () {
    try { return Math.max(0, parseInt(localStorage.getItem('mgf-hyeopgok-sasu-best-v32') || '0', 10) || 0); }
    catch (e) { return 0; }
  },
  HYEOPGOK_SaveBest: function (value) {
    try { localStorage.setItem('mgf-hyeopgok-sasu-best-v32', String(Math.max(0, value | 0))); }
    catch (e) {}
  },
  // Validation-only compositor hold. Normal players never define the global,
  // so the 1.12 s upgrade sequence advances without any extra state.
  HYEOPGOK_CaptureUpgradeProgress: function () {
    try {
      var value = Number(window.__HYEOPGOK_CAPTURE_UPGRADE__);
      return isFinite(value) && value >= 0 && value < 1 ? value : -1;
    } catch (e) { return -1; }
  }
});
