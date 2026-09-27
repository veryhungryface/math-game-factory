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
        var source = window.__HYEOPGOK_SAMPLE_BANK__[base.id] || {};
        var result = {};
        Object.keys(source).forEach(function (key) { result[key] = source[key]; });
        Object.keys(base).forEach(function (key) { result[key] = base[key]; });
        if (typeof base.answerNumeric !== 'number') delete result.answerNumeric;
        return JSON.parse(JSON.stringify(result));
      });
    };
    test.__hyeopgokBankWrapped = true;
  }
});
