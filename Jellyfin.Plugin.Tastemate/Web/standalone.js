// Lets the Tastemate page run outside the Jellyfin web client (and so for users who are not administrators).
// It provides the small part of the web client's ApiClient and Dashboard objects that the page uses, signed in
// with the session the web client keeps in localStorage.
(function () {
    'use strict';

    var base = location.pathname.replace(/\/Tastemate\/Page\/?$/i, '');
    var token = null;
    var userId = null;

    try {
        var credentials = JSON.parse(localStorage.getItem('jellyfin_credentials') || '{}');
        var servers = (credentials.Servers || []).filter(function (s) { return s.AccessToken && s.UserId; });
        servers.sort(function (a, b) { return new Date(b.DateLastAccessed || 0) - new Date(a.DateLastAccessed || 0); });
        var here = (location.origin + base).toLowerCase();
        var server = servers.filter(function (s) {
            return [s.ManualAddress, s.LocalAddress, s.RemoteAddress].some(function (a) { return a && a.replace(/\/+$/, '').toLowerCase() === here; });
        })[0] || servers[0];
        if (server) { token = server.AccessToken; userId = server.UserId; }
    } catch (e) { /* not signed in */ }

    function query(params) {
        var parts = Object.keys(params || {}).map(function (k) { return encodeURIComponent(k) + '=' + encodeURIComponent(params[k]); });
        return parts.length ? parts.join('&') : '';
    }

    function getUrl(name, params) {
        var q = query(params);
        return base + '/' + name + (q ? (name.indexOf('?') < 0 ? '?' : '&') + q : '');
    }

    function ajax(options) {
        var headers = { Authorization: 'MediaBrowser Client="Tastemate", Device="Browser", DeviceId="tastemate", Version="1", Token="' + token + '"' };
        if (options.contentType) { headers['Content-Type'] = options.contentType; }
        return fetch(options.url, { method: options.type || 'GET', headers: headers, body: options.data }).then(function (response) {
            if (!response.ok) { return Promise.reject(response); }
            if (options.dataType !== 'json') { return response; }
            return response.text().then(function (text) { return text ? JSON.parse(text) : null; });
        });
    }

    window.ApiClient = {
        getUrl: getUrl,
        ajax: ajax,
        getCurrentUserId: function () { return userId; },
        getCurrentUser: function () { return ajax({ type: 'GET', url: getUrl('Users/Me'), dataType: 'json' }); },
        getItems: function (id, params) { return ajax({ type: 'GET', url: getUrl('Users/' + id + '/Items', params), dataType: 'json' }); }
    };

    window.Dashboard = {
        alert: function (options) { window.alert(typeof options === 'string' ? options : options.message); },
        showLoadingMsg: function () { document.body.classList.add('tm-loading'); },
        hideLoadingMsg: function () { document.body.classList.remove('tm-loading'); }
    };

    var style = document.createElement('style');
    style.textContent = [
        'html { background: #101010; }',
        'body { margin: 0; padding: 1.5em; color: #eee; font: 16px/1.4 system-ui, -apple-system, "Segoe UI", sans-serif; }',
        '#TastematePage { max-width: 70em; margin: 0 auto; }',
        'body.tm-loading { cursor: progress; }',
        '.tm-back { display: inline-block; margin-bottom: 1em; color: #00a4dc; text-decoration: none; }',
        '.tm-back:hover { text-decoration: underline; }',
        '.inputContainer { margin-bottom: 1em; }',
        '.inputLabel { display: block; margin-bottom: .3em; opacity: .8; }',
        '#TastematePage input[type=text], #TastematePage input[type=number], #TastematePage input[type=search], #TastematePage input[type=password] {',
        '  width: 100%; box-sizing: border-box; padding: .6em .8em; border: 0; border-bottom: 1px solid rgba(255,255,255,.3);',
        '  background: rgba(255,255,255,.08); color: inherit; font-size: inherit; }',
        'h2, h3, h4 { font-weight: 500; }'
    ].join('\n');
    document.head.appendChild(style);

    document.addEventListener('DOMContentLoaded', function () {
        var page = document.querySelector('#TastematePage');
        var back = document.createElement('a');
        back.className = 'tm-back';
        back.href = base + '/web/';
        back.textContent = '← Back to Jellyfin';
        page.insertBefore(back, page.firstChild);

        if (!token) {
            var message = document.createElement('p');
            message.innerHTML = 'You are not signed in. <a class="tm-back" href="' + base + '/web/">Sign in to Jellyfin</a> first, then open Tastemate again.';
            page.appendChild(message);
            return;
        }
        page.dispatchEvent(new Event('pageshow'));
    });
})();
