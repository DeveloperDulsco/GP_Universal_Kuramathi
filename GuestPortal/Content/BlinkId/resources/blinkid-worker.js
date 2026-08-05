var Oe = Object.defineProperty;
var J = (e) => {
  throw TypeError(e);
};
var Le = (e, t, r) => t in e ? Oe(e, t, { enumerable: !0, configurable: !0, writable: !0, value: r }) : e[t] = r;
var B = (e, t, r) => Le(e, typeof t != "symbol" ? t + "" : t, r), $ = (e, t, r) => t.has(e) || J("Cannot " + r);
var m = (e, t, r) => ($(e, t, "read from private field"), r ? r.call(e) : t.get(e)), S = (e, t, r) => t.has(e) ? J("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, r), x = (e, t, r, o) => ($(e, t, "write to private field"), o ? o.call(e, r) : t.set(e, r), r), v = (e, t, r) => ($(e, t, "access private method"), r);
/**
 * @license
 * Copyright 2019 Google LLC
 * SPDX-License-Identifier: Apache-2.0
 */
const re = Symbol("Comlink.proxy"), Ae = Symbol("Comlink.endpoint"), Re = Symbol("Comlink.releaseProxy"), z = Symbol("Comlink.finalizer"), N = Symbol("Comlink.thrown"), ne = (e) => typeof e == "object" && e !== null || typeof e == "function", ke = {
  canHandle: (e) => ne(e) && e[re],
  serialize(e) {
    const { port1: t, port2: r } = new MessageChannel();
    return V(e, t), [r, [r]];
  },
  deserialize(e) {
    return e.start(), ze(e);
  }
}, Ue = {
  canHandle: (e) => ne(e) && N in e,
  serialize({ value: e }) {
    let t;
    return e instanceof Error ? t = {
      isError: !0,
      value: {
        message: e.message,
        name: e.name,
        stack: e.stack
      }
    } : t = { isError: !1, value: e }, [t, []];
  },
  deserialize(e) {
    throw e.isError ? Object.assign(new Error(e.value.message), e.value) : e.value;
  }
}, se = /* @__PURE__ */ new Map([
  ["proxy", ke],
  ["throw", Ue]
]);
function Me(e, t) {
  for (const r of e)
    if (t === r || r === "*" || r instanceof RegExp && r.test(t))
      return !0;
  return !1;
}
function V(e, t = globalThis, r = ["*"]) {
  t.addEventListener("message", function o(s) {
    if (!s || !s.data)
      return;
    if (!Me(r, s.origin)) {
      console.warn(`Invalid origin '${s.origin}' for comlink proxy`);
      return;
    }
    const { id: n, type: u, path: a } = Object.assign({ path: [] }, s.data), c = (s.data.argumentList || []).map(E);
    let i;
    try {
      const l = a.slice(0, -1).reduce((f, p) => f[p], e), d = a.reduce((f, p) => f[p], e);
      switch (u) {
        case "GET":
          i = d;
          break;
        case "SET":
          l[a.slice(-1)[0]] = E(s.data.value), i = !0;
          break;
        case "APPLY":
          i = d.apply(l, c);
          break;
        case "CONSTRUCT":
          {
            const f = new d(...c);
            i = le(f);
          }
          break;
        case "ENDPOINT":
          {
            const { port1: f, port2: p } = new MessageChannel();
            V(e, p), i = ce(f, [f]);
          }
          break;
        case "RELEASE":
          i = void 0;
          break;
        default:
          return;
      }
    } catch (l) {
      i = { value: l, [N]: 0 };
    }
    Promise.resolve(i).catch((l) => ({ value: l, [N]: 0 })).then((l) => {
      const [d, f] = j(l);
      t.postMessage(Object.assign(Object.assign({}, d), { id: n }), f), u === "RELEASE" && (t.removeEventListener("message", o), oe(t), z in e && typeof e[z] == "function" && e[z]());
    }).catch((l) => {
      const [d, f] = j({
        value: new TypeError("Unserializable return value"),
        [N]: 0
      });
      t.postMessage(Object.assign(Object.assign({}, d), { id: n }), f);
    });
  }), t.start && t.start();
}
function Te(e) {
  return e.constructor.name === "MessagePort";
}
function oe(e) {
  Te(e) && e.close();
}
function ze(e, t) {
  const r = /* @__PURE__ */ new Map();
  return e.addEventListener("message", function(s) {
    const { data: n } = s;
    if (!n || !n.id)
      return;
    const u = r.get(n.id);
    if (u)
      try {
        u(n);
      } finally {
        r.delete(n.id);
      }
  }), D(e, r, [], t);
}
function k(e) {
  if (e)
    throw new Error("Proxy has been released and is not useable");
}
function ae(e) {
  return O(e, /* @__PURE__ */ new Map(), {
    type: "RELEASE"
  }).then(() => {
    oe(e);
  });
}
const I = /* @__PURE__ */ new WeakMap(), W = "FinalizationRegistry" in globalThis && new FinalizationRegistry((e) => {
  const t = (I.get(e) || 0) - 1;
  I.set(e, t), t === 0 && ae(e);
});
function Ne(e, t) {
  const r = (I.get(t) || 0) + 1;
  I.set(t, r), W && W.register(e, t, e);
}
function Ie(e) {
  W && W.unregister(e);
}
function D(e, t, r = [], o = function() {
}) {
  let s = !1;
  const n = new Proxy(o, {
    get(u, a) {
      if (k(s), a === Re)
        return () => {
          Ie(n), ae(e), t.clear(), s = !0;
        };
      if (a === "then") {
        if (r.length === 0)
          return { then: () => n };
        const c = O(e, t, {
          type: "GET",
          path: r.map((i) => i.toString())
        }).then(E);
        return c.then.bind(c);
      }
      return D(e, t, [...r, a]);
    },
    set(u, a, c) {
      k(s);
      const [i, l] = j(c);
      return O(e, t, {
        type: "SET",
        path: [...r, a].map((d) => d.toString()),
        value: i
      }, l).then(E);
    },
    apply(u, a, c) {
      k(s);
      const i = r[r.length - 1];
      if (i === Ae)
        return O(e, t, {
          type: "ENDPOINT"
        }).then(E);
      if (i === "bind")
        return D(e, t, r.slice(0, -1));
      const [l, d] = K(c);
      return O(e, t, {
        type: "APPLY",
        path: r.map((f) => f.toString()),
        argumentList: l
      }, d).then(E);
    },
    construct(u, a) {
      k(s);
      const [c, i] = K(a);
      return O(e, t, {
        type: "CONSTRUCT",
        path: r.map((l) => l.toString()),
        argumentList: c
      }, i).then(E);
    }
  });
  return Ne(n, e), n;
}
function We(e) {
  return Array.prototype.concat.apply([], e);
}
function K(e) {
  const t = e.map(j);
  return [t.map((r) => r[0]), We(t.map((r) => r[1]))];
}
const ie = /* @__PURE__ */ new WeakMap();
function ce(e, t) {
  return ie.set(e, t), e;
}
function le(e) {
  return Object.assign(e, { [re]: !0 });
}
function j(e) {
  for (const [t, r] of se)
    if (r.canHandle(e)) {
      const [o, s] = r.serialize(e);
      return [
        {
          type: "HANDLER",
          name: t,
          value: o
        },
        s
      ];
    }
  return [
    {
      type: "RAW",
      value: e
    },
    ie.get(e) || []
  ];
}
function E(e) {
  switch (e.type) {
    case "HANDLER":
      return se.get(e.name).deserialize(e.value);
    case "RAW":
      return e.value;
  }
}
function O(e, t, r, o) {
  return new Promise((s) => {
    const n = je();
    t.set(n, s), e.start && e.start(), e.postMessage(Object.assign({ id: n }, r), o);
  });
}
function je() {
  return new Array(4).fill(0).map(() => Math.floor(Math.random() * Number.MAX_SAFE_INTEGER).toString(16)).join("-");
}
const Ce = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0, 5, 3, 1, 0, 1, 10, 14, 1, 12, 0, 65, 0, 65, 0, 65, 0, 252, 10, 0, 0, 11])), Be = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 2, 8, 1, 1, 97, 1, 98, 3, 127, 1, 6, 6, 1, 127, 1, 65, 0, 11, 7, 5, 1, 1, 97, 3, 1])), $e = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0, 10, 7, 1, 5, 0, 208, 112, 26, 11])), De = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0, 10, 12, 1, 10, 0, 67, 0, 0, 0, 0, 252, 0, 26, 11])), Ve = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0, 10, 8, 1, 6, 0, 65, 0, 192, 26, 11])), Fe = async () => WebAssembly.validate(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 5, 1, 96, 0, 1, 123, 3, 2, 1, 0, 10, 10, 1, 8, 0, 65, 0, 253, 15, 253, 98, 11])), He = () => (async (e) => {
  try {
    return typeof MessageChannel < "u" && new MessageChannel().port1.postMessage(new SharedArrayBuffer(1)), WebAssembly.validate(e);
  } catch {
    return !1;
  }
})(new Uint8Array([0, 97, 115, 109, 1, 0, 0, 0, 1, 4, 1, 96, 0, 0, 3, 2, 1, 0, 5, 4, 1, 3, 1, 1, 10, 11, 1, 9, 0, 65, 0, 254, 16, 2, 0, 26, 11]));
function _e() {
  const e = navigator.userAgent.toLowerCase();
  return e.includes("safari") && !e.includes("chrome");
}
async function qe() {
  if (!await He()) return !1;
  if (!("importScripts" in self))
    throw Error("Not implemented");
  return _e() ? !1 : "Worker" in self;
}
async function Ge() {
  const e = [
    Be(),
    $e(),
    Ce(),
    De(),
    Ve()
  ];
  if (!(await Promise.all(e)).every(Boolean))
    throw new Error("Browser doesn't meet minimum requirements!");
  return await Fe() ? await qe() ? "advanced-threads" : "advanced" : "basic";
}
const Y = "application/javascript", Je = (e, t = {}) => {
  const r = {
    skipSameOrigin: !0,
    useBlob: !0,
    ...t
  };
  return r.skipSameOrigin && new URL(e).origin === self.location.origin ? Promise.resolve(e) : new Promise(
    (o, s) => void fetch(e).then((n) => n.text()).then((n) => {
      new URL(e).href.split("/").pop();
      let a = "";
      if (r.useBlob) {
        const c = new Blob([n], { type: Y });
        a = URL.createObjectURL(c);
      } else
        a = `data:${Y},` + encodeURIComponent(n);
      o(a);
    }).catch(s)
  );
};
function Ke() {
  const e = self.navigator.userAgent.toLowerCase();
  return /iphone|ipad|ipod/.test(e);
}
function Ye(e) {
  return {
    licenseId: e.licenseId,
    licensee: e.licensee,
    applicationIds: e.applicationIds,
    packageName: e.packageName,
    platform: "Browser",
    sdkName: e.sdkName,
    sdkVersion: e.sdkVersion
  };
}
async function X(e, t = "https://baltazar.microblink.com/api/v2/status/check") {
  if (!t || typeof t != "string")
    throw new Error("Invalid baltazarUrl: must be a non-empty string");
  try {
    new URL(t);
  } catch {
    throw new Error(`Invalid baltazarUrl format: ${t}`);
  }
  try {
    const r = await fetch(t, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      cache: "no-cache",
      body: JSON.stringify(Ye(e))
    });
    if (!r.ok)
      throw new Error(
        `Server returned error: ${r.status} ${r.statusText}`
      );
    return await r.json();
  } catch (r) {
    throw console.error("Server permission request failed:", r), r;
  }
}
function Q(e) {
  return Math.ceil(e * 1024 * 1024 / 64 / 1024);
}
const Xe = { basic: { full: 3065196, lightweight: 3123869 }, advanced: { full: 3081572, lightweight: 3138602 }, "advanced-threads": { full: 3133721, lightweight: 3189140 } }, Qe = { basic: { full: 9141750, lightweight: 7509065 }, advanced: { full: 9141750, lightweight: 7509065 }, "advanced-threads": { full: 9141750, lightweight: 7509065 } }, Ze = {
  wasm: Xe,
  data: Qe
};
function et(e, t, r) {
  return Ze[e][t][r];
}
async function Z(e, t, r, o, s) {
  var d;
  const n = await fetch(e);
  if (!s)
    return n.arrayBuffer();
  const u = n.headers.get("Content-Length"), a = u ? parseInt(u, 10) : et(t, r, o);
  if (isNaN(a) || a <= 0)
    throw new Error(
      `Invalid content length for ${t} file: ${a}`
    );
  let c = 0;
  const i = new TransformStream({
    transform(f, p) {
      c += f.length;
      const C = Math.min(
        Math.round(c / a * 100),
        100
      );
      s({
        loaded: c,
        contentLength: a,
        progress: C,
        finished: !1
      }), p.enqueue(f);
    },
    flush() {
      s({
        loaded: c,
        contentLength: a,
        progress: 100,
        finished: !0
      });
    }
  });
  return new Response(
    (d = n.body) == null ? void 0 : d.pipeThrough(i),
    n
  ).arrayBuffer();
}
function U(...e) {
  const t = e.filter((r) => r).join("/").replace(/([^:]\/)\/+/g, "$1");
  try {
    new URL(t, "http://example.com");
  } catch {
    throw new Error(`Invalid URL: ${t}`);
  }
  return t;
}
function ue(e) {
  return Object.prototype.toString.call(e).slice(8, -1);
}
function M(e) {
  if (ue(e) !== "Object")
    return !1;
  const t = Object.getPrototypeOf(e);
  return !!t && t.constructor === Object && t === Object.prototype;
}
function ee(e) {
  return ue(e) === "Symbol";
}
function te(e, t, r, o) {
  const s = {}.propertyIsEnumerable.call(o, t) ? "enumerable" : "nonenumerable";
  s === "enumerable" && (e[t] = r), s === "nonenumerable" && Object.defineProperty(e, t, {
    value: r,
    enumerable: !1,
    writable: !0,
    configurable: !0
  });
}
function de(e, t, r) {
  if (!M(t))
    return t;
  let o = {};
  if (M(e)) {
    const a = Object.getOwnPropertyNames(e), c = Object.getOwnPropertySymbols(e);
    o = [...a, ...c].reduce((i, l) => {
      const d = e[l];
      return (!ee(l) && !Object.getOwnPropertyNames(t).includes(l) || ee(l) && !Object.getOwnPropertySymbols(t).includes(l)) && te(i, l, d, e), i;
    }, {});
  }
  const s = Object.getOwnPropertyNames(t), n = Object.getOwnPropertySymbols(t);
  return [...s, ...n].reduce((a, c) => {
    let i = t[c];
    const l = M(e) ? e[c] : void 0;
    return l !== void 0 && M(i) && (i = de(l, i)), te(a, c, i, t), a;
  }, o);
}
function tt(e, ...t) {
  return t.reduce((r, o) => de(r, o), e);
}
function fe(e) {
  return {
    country: (e == null ? void 0 : e.country) ?? void 0,
    region: (e == null ? void 0 : e.region) ?? void 0,
    type: (e == null ? void 0 : e.type) ?? void 0
  };
}
const rt = (e) => ({
  documentFilter: fe(e.documentFilter),
  fields: e.fields ?? []
}), nt = (e) => ({
  documentFilter: fe(e.documentFilter),
  fields: e.fields || [],
  documentNumberAnonymizationSettings: e.documentNumberAnonymizationSettings ? {
    prefixDigitsVisible: e.documentNumberAnonymizationSettings.prefixDigitsVisible,
    suffixDigitsVisible: e.documentNumberAnonymizationSettings.suffixDigitsVisible
  } : void 0
});
function st(e = {}, t) {
  var u, a, c, i;
  e && (e = Object.fromEntries(
    Object.entries(e).filter(([l, d]) => d !== void 0)
  ));
  const r = ((a = (u = e == null ? void 0 : e.scanningSettings) == null ? void 0 : u.customDocumentRules) == null ? void 0 : a.map(
    rt
  )) ?? [], o = ((i = (c = e == null ? void 0 : e.scanningSettings) == null ? void 0 : c.customDocumentAnonymizationSettings) == null ? void 0 : i.map(
    nt
  )) ?? [], s = {
    ...e == null ? void 0 : e.scanningSettings,
    customDocumentRules: r,
    customDocumentAnonymizationSettings: o
  };
  return tt(t, {
    ...e,
    scanningSettings: s
  });
}
class T extends Error {
  constructor(t, r) {
    super(`Proxy URL validation failed for "${r}": ${t}`), this.url = r, this.name = "ProxyUrlValidationError";
  }
}
class ot extends Error {
  constructor(r, o) {
    super(r);
    B(this, "code");
    this.name = "LicenseError", this.code = o;
  }
}
var h, L, A, R, w, g, me, he, ge, ye, pe;
class at {
  constructor() {
    S(this, g);
    /**
     * The Wasm module.
     */
    S(this, h);
    /**
     * The default session settings.
     *
     * Must be initialized when calling initBlinkId.
     */
    S(this, L);
    /**
     * The progress status callback.
     */
    B(this, "progressStatusCallback");
    /**
     * Whether the demo overlay is shown.
     */
    S(this, A, !0);
    /**
     * Whether the production overlay is shown.
     */
    S(this, R, !0);
    /**
     * Sanitized proxy URLs for Microblink services.
     */
    S(this, w);
  }
  /**
   * This method initializes everything.
   */
  async initBlinkId(t, r, o) {
    var u;
    const s = new URL(
      "resources/",
      t.resourcesLocation
    ).toString();
    if (x(this, L, r), this.progressStatusCallback = o, await v(this, g, me).call(this, {
      resourceUrl: s,
      variant: t.wasmVariant,
      initialMemory: t.initialMemory,
      useLightweightBuild: t.useLightweightBuild
    }), !m(this, h))
      throw new Error("Wasm module not loaded");
    const n = m(this, h).initializeWithLicenseKey(
      t.licenseKey,
      t.userId,
      !1
    );
    if (n.licenseError)
      throw new ot(
        "License unlock error: " + n.licenseError,
        "LICENSE_ERROR"
      );
    if (t.microblinkProxyUrl && v(this, g, he).call(this, t.microblinkProxyUrl, n), n.unlockResult === "requires-server-permission") {
      const a = (u = m(this, w)) == null ? void 0 : u.baltazar, c = a && n.allowBaltazarProxy ? await X(n, a) : await X(n), i = m(this, h).submitServerPermission(
        JSON.stringify(c)
      );
      if (i.error)
        throw new Error("Server unlock error: " + i.error);
    }
    x(this, A, n.showDemoOverlay), x(this, R, n.showProductionOverlay);
  }
  /**
   * This method creates a BlinkID scanning session.
   *
   * @param options - The options for the session.
   * @returns The session.
   */
  createBlinkIdScanningSession(t) {
    if (!m(this, h))
      throw new Error("Wasm module not loaded");
    const r = st(
      t,
      m(this, L)
    ), o = m(this, h).createBlinkIdScanningSession(r);
    return this.createProxySession(o, r);
  }
  /**
   * This method creates a proxy session.
   *
   * @param session - The session.
   * @param sessionSettings - The session settings.
   * @returns The proxy session.
   */
  createProxySession(t, r) {
    return le({
      getResult: () => t.getResult(),
      process: (s) => {
        const n = t.process(s);
        if ("error" in n)
          throw new Error(`Error processing frame: ${n.error}`);
        return ce(
          {
            ...n,
            arrayBuffer: s.data.buffer
          },
          [s.data.buffer]
        );
      },
      getSettings: () => r,
      reset: () => t.reset(),
      delete: () => t.delete(),
      deleteLater: () => t.deleteLater(),
      isDeleted: () => t.isDeleted(),
      isAliasOf: (s) => t.isAliasOf(s),
      showDemoOverlay: () => m(this, A),
      showProductionOverlay: () => m(this, R)
    });
  }
  /**
   * This method is called when the worker is terminated.
   */
  [z]() {
  }
  /**
   * Terminates the workers and the Wasm runtime.
   */
  terminate() {
    self.close();
  }
  /**
   * If the ping is enabled, this method will return 1.
   *
   * @returns 1 if the ping is enabled, 0 otherwise.
   */
  ping() {
    return 1;
  }
}
h = new WeakMap(), L = new WeakMap(), A = new WeakMap(), R = new WeakMap(), w = new WeakMap(), g = new WeakSet(), me = async function({
  resourceUrl: t,
  variant: r,
  useLightweightBuild: o,
  initialMemory: s
}) {
  if (m(this, h)) {
    console.log("Wasm already loaded");
    return;
  }
  const n = r ?? await Ge(), u = o ? "lightweight" : "full", a = "BlinkIdModule", c = U(
    t,
    u,
    n
  ), i = U(c, `${a}.js`), l = U(c, `${a}.wasm`), d = U(c, `${a}.data`), f = await Je(i), C = (await import(
    /* @vite-ignore */
    f
  )).default;
  s || (s = Ke() ? 700 : 200);
  const we = new WebAssembly.Memory({
    initial: Q(s),
    maximum: Q(2048),
    shared: n === "advanced-threads"
  });
  let b, P, F = 0;
  const be = 32, H = () => {
    if (!this.progressStatusCallback || !b || !P)
      return;
    const y = b.finished && P.finished, _ = b.loaded + P.loaded, q = b.contentLength + P.contentLength, ve = y ? 100 : Math.min(Math.round(_ / q * 100), 100), G = performance.now();
    G - F < be || (F = G, this.progressStatusCallback({
      loaded: _,
      contentLength: q,
      progress: ve,
      finished: y
    }));
  }, Pe = (y) => {
    b = y, H();
  }, Se = (y) => {
    P = y, H();
  }, [Ee, xe] = await Promise.all([
    Z(
      l,
      "wasm",
      n,
      u,
      Pe
    ),
    Z(
      d,
      "data",
      n,
      u,
      Se
    )
  ]);
  if (this.progressStatusCallback && b && P) {
    const y = b.contentLength + P.contentLength;
    this.progressStatusCallback({
      loaded: y,
      contentLength: y,
      progress: 100,
      finished: !0
    });
  }
  if (x(this, h, await C({
    locateFile: (y) => `${c}/${n}/${y}`,
    // pthreads build breaks without this:
    // "Failed to execute 'createObjectURL' on 'URL': Overload resolution failed."
    mainScriptUrlOrBlob: f,
    wasmBinary: Ee,
    getPreloadedPackage() {
      return xe;
    },
    wasmMemory: we,
    noExitRuntime: !0
  })), !m(this, h))
    throw new Error("Failed to load Wasm module");
}, /**
 * Configures proxy URLs based on the provided settings and license permissions.
 */
he = function(t, r) {
  if (!t) {
    console.debug(
      "No proxy URL configured, using default Microblink servers"
    );
    return;
  }
  v(this, g, ge).call(this, r, t);
  try {
    x(this, w, v(this, g, ye).call(this, t)), r.allowPingProxy && m(this, h).setPingProxyUrl(m(this, w).ping), console.debug("Proxy URLs configured successfully:", {
      ping: m(this, w).ping,
      baltazar: m(this, w).baltazar
    });
  } catch (o) {
    throw o instanceof T ? new Error(
      `${o.message}

Troubleshooting:
- Ensure the URL is accessible
- Check HTTPS requirements
- Verify proxy server implementation`
    ) : o;
  }
}, /**
 * Validates that the license allows proxy usage.
 */
ge = function(t, r) {
  if (!t.allowPingProxy && !t.allowBaltazarProxy)
    throw new Error(
      `Proxy URL "${r}" was provided, but your license does not permit proxy usage.
License permissions: pingProxy=${t.allowPingProxy}, baltazarProxy=${t.allowBaltazarProxy}
Check your license.`
    );
  if (!t.hasPing && t.unlockResult !== "requires-server-permission")
    throw new Error(
      `Microblink proxy URL is set but cannot be used because ping and online license check are disabled in your license.
Check your license.`
    );
}, /**
 * Validates and sanitizes proxy URLs for different Microblink services.
 */
ye = function(t) {
  let r;
  try {
    r = new URL(t);
  } catch {
    throw new T(
      `Failed to create URL instance for provided Microblink proxy URL "${t}". Expected format: https://your-proxy.com or https://your-proxy.com/`,
      t
    );
  }
  if (r.protocol !== "https:")
    throw new T(
      `Proxy URL validation failed for "${t}": HTTPS protocol must be used. Expected format: https://your-proxy.com or https://your-proxy.com/`,
      t
    );
  const o = r.origin, s = v(this, g, pe).call(this, o, "/api/v2/status/check");
  return {
    ping: o,
    baltazar: s
  };
}, /**
 * Builds a service URL by combining base URL with service path.
 */
pe = function(t, r) {
  try {
    return new URL(r, t).toString();
  } catch {
    throw new T(
      `Failed to build service URL for path "${r}"`,
      t
    );
  }
};
const it = new at();
V(it);
