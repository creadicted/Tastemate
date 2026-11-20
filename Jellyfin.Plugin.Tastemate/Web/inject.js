// Adds a "Tastemate" entry to the Jellyfin web client's user menu. Loaded by index.html through the File Transformation plugin.
(function () {
    'use strict';

    var script = document.currentScript;
    var base = script && script.src ? script.src.replace(/\/Tastemate\/inject\.js.*$/i, '') : '';
    var href = base + '/Tastemate/Page';
    var MARK = 'data-tastemate-menu';
    // Material "format list numbered" icon
    var ICON = '<svg class="MuiSvgIcon-root" focusable="false" aria-hidden="true" viewBox="0 0 24 24" width="24" height="24" fill="currentColor"><path d="M2 17h2v.5H3v1h1v.5H2v1h3v-4H2zm1-9h1V4H2v1h1zm-1 3h1.8L2 13.1v.9h3v-1H3.2L5 10.9V10H2zm5-6v2h14V5zm0 14h14v-2H7zm0-6h14v-2H7z"></path></svg>';
    var queued = false;

    function addItems() {
        queued = false;
        var anchors = document.querySelectorAll('a[href*="mypreferencesmenu"]');
        Array.prototype.forEach.call(anchors, function (anchor) {
            var next = anchor.nextElementSibling;
            if (anchor.hasAttribute(MARK) || (next && next.hasAttribute(MARK))) { return; }
            // Copy the preferences entry so the item matches the current layout and theme
            var item = anchor.cloneNode(true);
            item.setAttribute(MARK, 'true');
            item.setAttribute('href', href);
            item.removeAttribute('aria-current');
            item.classList.remove('Mui-selected');

            var leaves = [];
            var walker = document.createTreeWalker(item, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) {
                if (walker.currentNode.nodeValue.trim()) { leaves.push(walker.currentNode); }
            }
            if (leaves.length) { leaves[leaves.length - 1].nodeValue = 'Tastemate'; }

            var icon = item.querySelector('svg, .material-icons, .navMenuOptionIcon');
            if (icon) {
                var holder = document.createElement('span');
                holder.innerHTML = ICON;
                var svg = holder.firstChild;
                if (icon.tagName.toLowerCase() === 'svg') {
                    icon.replaceWith(svg);
                } else {
                    icon.className = icon.className.replace(/\b(?!material-icons\b|navMenuOptionIcon\b)[\w-]+/g, '').trim();
                    icon.textContent = 'format_list_numbered';
                }
            }
            anchor.after(item);
        });
    }

    function queue() {
        if (queued) { return; }
        queued = true;
        window.requestAnimationFrame(addItems);
    }

    new MutationObserver(queue).observe(document.body || document.documentElement, { childList: true, subtree: true });
    queue();
})();
