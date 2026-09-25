// MGF Unity kit — C# → JS. 받는 쪽은 WebGL 템플릿(WebGLTemplates/MGF/index.html)의 window.__MGF_HOST__.
mergeInto(LibraryManager.library, {
  MGF_Ready: function () {
    var h = window.__MGF_HOST__;
    if (h && h.onReady) h.onReady();
  },
  MGF_PushState: function (p) {
    var h = window.__MGF_HOST__;
    if (h && h.onState) h.onState(UTF8ToString(p));
  },
  MGF_PushBank: function (p) {
    var h = window.__MGF_HOST__;
    if (h && h.onBank) h.onBank(UTF8ToString(p));
  },
  MGF_LowGfx: function () {
    try { return window.__MGF_HOST__ && window.__MGF_HOST__.lowGfx ? 1 : 0; } catch (e) { return 0; }
  },
  MGF_InitialMuted: function () {
    try { return window.__MGF_HOST__ && window.__MGF_HOST__.muted ? 1 : 0; } catch (e) { return 0; }
  }
});
