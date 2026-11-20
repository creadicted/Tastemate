// Adds a "Tastemate" entry to the Jellyfin web client's user menu. Loaded by index.html through the File Transformation plugin.
(function () {
    'use strict';

    // Never run twice, two observers would add the entry twice
    if (window.tastemateMenuLoaded) { return; }
    window.tastemateMenuLoaded = true;

    var script = document.currentScript;
    var base = script && script.src ? script.src.replace(/\/Tastemate\/inject\.js.*$/i, '') : '';
    var href = base + '/Tastemate/Page';
    var MARK = 'data-tastemate-menu';
    // The Settings entry of the drawer or the user menu, depending on the web client layout
    var SETTINGS = 'a[href*="mypreferencesmenu"], a.btnSettings, a[data-itemid="settings"]';
    // Material "format list numbered" icon
    var ICON = '<svg class="MuiSvgIcon-root" focusable="false" aria-hidden="true" viewBox="0 0 24 24" width="24" height="24" fill="currentColor"><path d="M2 17h2v.5H3v1h1v.5H2v1h3v-4H2zm1-9h1V4H2v1h1zm-1 3h1.8L2 13.1v.9h3v-1H3.2L5 10.9V10H2zm5-6v2h14V5zm0 14h14v-2H7zm0-6h14v-2H7z"></path></svg>';
    var queued = false;

    function createItem(anchor) {
        // Copy the Settings entry so the item matches the current layout and theme
        var item = anchor.cloneNode(true);
        item.setAttribute(MARK, 'true');
        item.setAttribute('href', href);
        item.removeAttribute('aria-current');
        item.removeAttribute('data-itemid');
        item.classList.remove('Mui-selected', 'btnSettings');

        var leaves = [];
        var walker = document.createTreeWalker(item, NodeFilter.SHOW_TEXT);
        while (walker.nextNode()) {
            if (walker.currentNode.nodeValue.trim()) { leaves.push(walker.currentNode); }
        }
        if (leaves.length) { leaves[leaves.length - 1].nodeValue = 'Tastemate'; }

        var icon = item.querySelector('svg, .material-icons, .navMenuOptionIcon');
        if (icon) {
            if (icon.tagName.toLowerCase() === 'svg') {
                var holder = document.createElement('span');
                holder.innerHTML = ICON;
                icon.replaceWith(holder.firstChild);
            } else {
                // Keep the font classes, drop the one that names the Settings glyph
                Array.prototype.slice.call(icon.classList).forEach(function (name) {
                    if (name !== 'material-icons' && name !== 'navMenuOptionIcon') { icon.classList.remove(name); }
                });
                // The web client names a Material icon by a class
                icon.classList.add('format_list_numbered');
            }
        }
        // The web client's own link handling would treat the address as one of its pages
        item.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            window.location.href = href;
        }, true);
        return item;
    }

    function addItems() {
        queued = false;
        var parents = [];

        // At most one entry per menu, whatever the web client re-rendered in the meantime
        Array.prototype.forEach.call(document.querySelectorAll('[' + MARK + ']'), function (item) {
            if (parents.indexOf(item.parentElement) >= 0) {
                item.remove();
            } else {
                parents.push(item.parentElement);
            }
        });

        Array.prototype.forEach.call(document.querySelectorAll(SETTINGS), function (anchor) {
            if (anchor.hasAttribute(MARK) || parents.indexOf(anchor.parentElement) >= 0) { return; }
            parents.push(anchor.parentElement);
            anchor.after(createItem(anchor));
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
