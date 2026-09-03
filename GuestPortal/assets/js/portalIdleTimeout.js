/**
 * Guest portal idle / request timeout.
 * After a long idle, Next/save used to spin forever (no fetch timeout, session HTML instead of JSON).
 */
(function (window, document) {
    var IDLE_MS = window.GP_PORTAL_IDLE_MS || (20 * 60 * 1000);
    var REQ_MS = window.GP_PORTAL_REQUEST_MS || (60 * 1000);
    var MSG = 'Your session has timed out because this page was idle. Please reopen your guest portal link and try again.';
    var lastActivity = Date.now();
    var shown = false;

    function bump(e) {
        if (e && e.isTrusted === false) {
            return;
        }
        lastActivity = Date.now();
        shown = false;
    }

    // Do not listen to scroll/change: layout scroll and form.valid() were resetting idle so Next never timed out.
    ['keydown', 'input', 'touchstart'].forEach(function (ev) {
        document.addEventListener(ev, bump, { passive: true, capture: true });
    });

    function isIdle() {
        return (Date.now() - lastActivity) > IDLE_MS;
    }

    function hideLoader() {
        try {
            if (window.jQuery) {
                window.jQuery('.loader-screen').stop(true, true).hide();
            }
        } catch (e) { }
    }

    function showTimeout(customMsg) {
        hideLoader();
        if (shown) {
            return;
        }
        shown = true;
        var text = customMsg || MSG;
        try {
            if (window.jQuery && window.jQuery('#customMessageModal').length) {
                window.jQuery('#customMessageModalMessage').html(text);
                window.jQuery('#customMessageModal').modal({ backdrop: 'static', keyboard: false, show: true });
                return;
            }
        } catch (e) { }
        try { window.alert(text); } catch (e2) { }
    }

    window.gpPortalIdleExpired = isIdle;
    window.gpShowPortalTimeout = showTimeout;
    window.gpGuardPortalSave = function () {
        if (isIdle()) {
            showTimeout();
            return false;
        }
        return true;
    };

    function isLongRunningUrl(url) {
        return /ProcessDocument|uploadDocument|Blink|Microblink|ocr/i.test(url || '');
    }

    function requestUrl(input) {
        if (typeof input === 'string') {
            return input;
        }
        if (input && input.url) {
            return input.url;
        }
        return '';
    }

    if (window.fetch) {
        var origFetch = window.fetch;
        window.fetch = function (input, init) {
            init = init ? init : {};
            if (isIdle()) {
                showTimeout();
                return Promise.reject(new Error('idle-timeout'));
            }
            var url = requestUrl(input);
            var waitMs = isLongRunningUrl(url) ? Math.max(REQ_MS, 180000) : REQ_MS;
            var ctrl = null;
            if (typeof AbortController !== 'undefined' && !init.signal) {
                ctrl = new AbortController();
                init.signal = ctrl.signal;
                setTimeout(function () {
                    try { ctrl.abort(); } catch (e) { }
                }, waitMs);
            }
            return origFetch.call(this, input, init).then(function (res) {
                var ct = '';
                try { ct = (res.headers && res.headers.get('content-type')) || ''; } catch (e) { }
                if (res.status === 401 || res.status === 403) {
                    showTimeout();
                } else if (ct.toLowerCase().indexOf('text/html') >= 0) {
                    showTimeout();
                }
                return res;
            }).catch(function (err) {
                var name = (err && err.name) || '';
                var msg = (err && err.message) || '';
                if (name === 'AbortError' || msg === 'idle-timeout' || msg === 'Failed to fetch' || msg === 'NetworkError when attempting to fetch resource.') {
                    showTimeout();
                }
                throw err;
            });
        };
    }

    if (window.jQuery) {
        window.jQuery(document).ajaxError(function (event, xhr, settings, thrown) {
            if (!settings || settings.crossDomain) {
                return;
            }
            var status = (thrown || '').toString().toLowerCase();
            var http = xhr ? xhr.status : 0;
            if (status === 'timeout' || status === 'abort' || http === 0 || http === 401 || http === 403) {
                showTimeout();
            }
        });
    }
})(window, document);
