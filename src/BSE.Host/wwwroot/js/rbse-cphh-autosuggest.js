(function () {
    var MAX_LOCAL_VALUES = 20;
    var MAX_RBSE_LOCAL_VALUES = 10;
    var MAX_RENDER = 20;
    var preloaded = { rbse: [], cphh: [] };

    function normalizeValue(field, value) {
        if (!value) return '';
        var raw = String(value).trim();
        if (!raw) return '';

        if (field === 'rbse') {
            var digits = raw.replace(/[^0-9]/g, '');
            if (digits.length === 9) return digits.slice(0, 2) + '/' + digits.slice(2, 4) + '/' + digits.slice(4);
            return raw.toUpperCase();
        }

        var c = raw.replace(/[^0-9]/g, '');
        if (c.length === 11) return c.slice(0, 2) + '/' + c.slice(2, 5) + '/' + c.slice(5, 9) + '/' + c.slice(9);
        return raw.toUpperCase();
    }

    function localKeys(field) {
        if (field === 'rbse') {
            // Keep compatibility with Home page legacy key.
            return ['bse.suggest.rbse', 'bse.recent.rbse'];
        }

        if (field === 'cphh') {
            return ['bse.suggest.cphh', 'bse.recent.cphh'];
        }

        return ['bse.suggest.' + field];
    }

    function readLocal(field) {
        try {
            var keys = localKeys(field);
            var all = [];

            keys.forEach(function (k) {
                var json = localStorage.getItem(k);
                if (!json) return;
                var arr = JSON.parse(json);
                if (Array.isArray(arr)) {
                    all = all.concat(arr.filter(Boolean));
                }
            });

            return unique(all);
        } catch {
            return [];
        }
    }

    function writeLocal(field, values) {
        try {
            var max = field === 'rbse' ? MAX_RBSE_LOCAL_VALUES : MAX_LOCAL_VALUES;
            var trimmed = values.slice(0, max);

            // Primary autosuggest key.
            localStorage.setItem('bse.suggest.' + field, JSON.stringify(trimmed));

            // Mirror RBSE into legacy home key so lists appear instantly across pages.
            if (field === 'rbse') {
                var raw = trimmed.map(function (v) { return String(v || '').replace(/\D/g, '').trim(); }).filter(Boolean);
                localStorage.setItem('bse.recent.rbse', JSON.stringify(raw));
            }

            if (field === 'cphh') {
                localStorage.setItem('bse.recent.cphh', JSON.stringify(trimmed));
            }
        } catch { }
    }

    function addLocal(field, value) {
        var normalized = normalizeValue(field, value);
        if (!normalized) return;
        var existing = readLocal(field);
        var merged = [normalized].concat(existing.filter(function (x) { return x.toLowerCase() !== normalized.toLowerCase(); }));
        writeLocal(field, merged);
    }

    async function preloadField(field) {
        try {
            var remote = await fetchValues(field, '');
            preloaded[field] = unique(remote || []);
            if (preloaded[field].length) {
                var existing = readLocal(field);
                writeLocal(field, unique(existing.concat(preloaded[field])));
            }
        } catch { }
    }

    function inferField(input) {
        var parts = [input.name || '', input.id || '', input.getAttribute('aria-label') || '', input.placeholder || ''];
        var label = document.querySelector('label[for="' + (input.id || '') + '"]');
        if (label) parts.push(label.textContent || '');
        var text = parts.join(' ').toLowerCase();

        if (text.indexOf('rbse') >= 0) return 'rbse';
        if (text.indexOf('cphh') >= 0 || text.indexOf('cph(h)') >= 0) return 'cphh';
        return null;
    }

    function isEligible(input) {
        if (!input || input.tagName !== 'INPUT') return false;
        if (input.dataset.noGlobalAutosuggest === 'true') return false;
        var type = (input.type || 'text').toLowerCase();
        if (!(type === 'text' || type === 'search' || type === '')) return false;
        if (input.disabled || input.readOnly) return false;
        if (input.closest('#rbse-combobox')) return false;
        return inferField(input) !== null;
    }

    async function fetchValues(field, query) {
        var url = '/Api/Suggestions?field=' + encodeURIComponent(field) + '&query=' + encodeURIComponent(query || '') + '&limit=20';
        try {
            var response = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!response.ok) return [];
            var json = await response.json();
            return Array.isArray(json.values) ? json.values : [];
        } catch {
            return [];
        }
    }

    function unique(values) {
        var set = new Set();
        var result = [];
        values.forEach(function (v) {
            if (!v) return;
            var key = String(v).toLowerCase();
            if (set.has(key)) return;
            set.add(key);
            result.push(v);
        });
        return result;
    }

    function createListBox(ownerId, field) {
        var box = document.createElement('ul');
        box.className = 'bse-autosuggest-list';
        box.setAttribute('data-bse-autosuggest-floating', 'true');
        box.setAttribute('data-bse-owner', ownerId);
        box.setAttribute('data-bse-field', field);
        box.setAttribute('role', 'listbox');
        box.hidden = true;
        document.body.appendChild(box);
        return box;
    }

    function createSavedPanel(field, ownerId) {
        var panel = document.createElement('div');
        panel.className = 'bse-autosuggest-panel';
        panel.setAttribute('data-bse-owner', ownerId);
        panel.setAttribute('data-bse-field', field);
        panel.hidden = true;

        var header = document.createElement('div');
        header.className = 'bse-autosuggest-panel__header';

        var title = document.createElement('strong');
        title.textContent = 'Saved info';

        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'bse-autosuggest-panel__close';
        close.setAttribute('aria-label', field === 'cphh' ? 'Close saved CPHH list' : 'Close saved RBSE list');
        close.textContent = '×';

        var list = document.createElement('ul');
        list.className = 'bse-autosuggest-list bse-autosuggest-list--rbse';
        list.setAttribute('role', 'listbox');

        header.appendChild(title);
        header.appendChild(close);
        panel.appendChild(header);
        panel.appendChild(list);
        document.body.appendChild(panel);

        return { panel: panel, close: close, list: list };
    }

    function createFormatHint(field, ownerId) {
        var hint = document.createElement('div');
        hint.className = 'bse-autosuggest-format-hint';
        hint.setAttribute('data-bse-owner', ownerId);
        hint.setAttribute('data-bse-field', field);
        hint.textContent = field === 'cphh'
            ? 'CPHH Format: NN/NNN/NNNN/NN'
            : 'RBSE Format: NN/NN/NNNNN';
        hint.hidden = true;
        document.body.appendChild(hint);
        return hint;
    }

    function positionList(input, list) {
        var r = input.getBoundingClientRect();
        list.style.left = (window.scrollX + r.left) + 'px';
        list.style.top = (window.scrollY + r.bottom + 2) + 'px';
        list.style.width = r.width + 'px';
    }

    function positionPanel(input, panel) {
        var r = input.getBoundingClientRect();
        panel.style.left = (window.scrollX + r.left) + 'px';
        panel.style.top = (window.scrollY + r.bottom + 6) + 'px';
    }

    function positionHint(input, hint) {
        var r = input.getBoundingClientRect();
        hint.style.left = (window.scrollX + r.left + 14) + 'px';
        hint.style.top = (window.scrollY + r.bottom + 10) + 'px';
    }

    function attachAutosuggest(input) {
        if (input.dataset.bseAutosuggestBound === 'true') {
            return;
        }
        input.dataset.bseAutosuggestBound = 'true';

        if (!input.id) {
            input.id = 'bse-autosuggest-' + Math.random().toString(36).slice(2);
        }
        var ownerId = input.id;

        var field = inferField(input);
        if (!field) return;

        // Defensive cleanup: remove stale floating UI already associated with this input.
        document.querySelectorAll('[data-bse-owner="' + ownerId + '"]').forEach(function (el) { el.remove(); });

        var useSavedPanel = field === 'rbse' || field === 'cphh';
        var panelBundle = useSavedPanel ? createSavedPanel(field, ownerId) : null;
        var popup = panelBundle ? panelBundle.panel : createListBox(ownerId, field);
        var list = panelBundle ? panelBundle.list : popup;
        var formatHint = useSavedPanel ? createFormatHint(field, ownerId) : null;
        var items = [];
        var activeIndex = -1;
        var hideTimeout = null;

        function hideOtherPopupsForField() {
            // Remove any previously created autosuggest overlays for this field,
            // keeping only the current input's UI elements.
            document.querySelectorAll('.bse-autosuggest-panel, .bse-autosuggest-format-hint, .bse-autosuggest-list[data-bse-autosuggest-floating="true"]').forEach(function (el) {
                if (el === popup || el === formatHint) return;
                var elField = el.getAttribute('data-bse-field');
                if (!elField || elField === field) {
                    el.remove();
                }
            });
        }

        function hide() {
            popup.hidden = true;
            if (formatHint) formatHint.hidden = true;
            list.innerHTML = '';
            items = [];
            activeIndex = -1;
            input.removeAttribute('aria-expanded');
        }

        function showHintOnly() {
            if (!formatHint) return;
            hideOtherPopupsForField();
            popup.hidden = true;
            formatHint.hidden = false;
            positionHint(input, formatHint);
            input.setAttribute('aria-expanded', 'false');
            input.removeAttribute('aria-activedescendant');
            activeIndex = -1;
        }

        function setActive(index) {
            activeIndex = index;
            Array.from(list.children).forEach(function (li, i) {
                li.classList.toggle('bse-autosuggest-list__item--active', i === activeIndex);
            });
        }

        function apply(value) {
            input.value = value;
            addLocal(field, value);
            hide();
            input.dispatchEvent(new Event('change', { bubbles: true }));
        }

        function render(values) {
            items = unique(values).slice(0, MAX_RENDER);
            list.innerHTML = '';

            if (items.length === 0) {
                if (useSavedPanel) {
                    showHintOnly();
                } else {
                    hide();
                }
                return;
            }

            items.forEach(function (value, idx) {
                var li = document.createElement('li');
                li.className = 'bse-autosuggest-list__item';
                li.setAttribute('role', 'option');
                li.textContent = value;
                li.addEventListener('mousedown', function (e) {
                    e.preventDefault();
                    apply(value);
                });
                li.addEventListener('mouseenter', function () { setActive(idx); });
                list.appendChild(li);
            });

            if (panelBundle) {
                positionPanel(input, popup);
            } else {
                positionList(input, popup);
            }
            hideOtherPopupsForField();
            if (formatHint) formatHint.hidden = true;
            popup.hidden = false;
            input.setAttribute('aria-expanded', 'true');
        }

        async function loadAndShow() {
            var local = unique(readLocal(field).concat(preloaded[field] || []));

            // Show something immediately on first interaction (legacy-like responsiveness).
            render(unique(local));

            var remote = await fetchValues(field, input.value || '');
            var merged = unique(local.concat(remote));
            var changed = merged.length !== items.length
                || merged.some(function (v, i) { return v !== items[i]; });

            if (changed) {
                render(merged);
            }
        }

        if (useSavedPanel) {
            input.addEventListener('mouseenter', function () {
                if (popup.hidden) showHintOnly();
            });
            input.addEventListener('mouseleave', function () {
                if (popup.hidden && formatHint) formatHint.hidden = true;
            });
            input.addEventListener('focus', function () {
                if (popup.hidden) showHintOnly();
            });
            input.addEventListener('click', function () { void loadAndShow(); });
        } else {
            input.addEventListener('focus', function () { void loadAndShow(); });
            input.addEventListener('click', function () { void loadAndShow(); });
        }
        input.addEventListener('input', function () { void loadAndShow(); });

        input.addEventListener('keydown', function (e) {
            if (popup.hidden || items.length === 0) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                setActive(Math.min(items.length - 1, activeIndex + 1));
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                setActive(Math.max(0, activeIndex - 1));
            } else if (e.key === 'Enter') {
                if (activeIndex >= 0 && activeIndex < items.length) {
                    e.preventDefault();
                    apply(items[activeIndex]);
                }
            } else if (e.key === 'Escape') {
                hide();
            }
        });

        input.addEventListener('blur', function () {
            addLocal(field, input.value);
            clearTimeout(hideTimeout);
            hideTimeout = setTimeout(hide, 120);
        });

        if (panelBundle) {
            panelBundle.close.addEventListener('click', function () {
                hide();
                input.focus();
            });
        }

        window.addEventListener('resize', function () {
            if (popup.hidden) {
                if (formatHint && !formatHint.hidden) positionHint(input, formatHint);
                return;
            }
            if (panelBundle) positionPanel(input, popup);
            else positionList(input, popup);
        });
        window.addEventListener('scroll', function () {
            if (popup.hidden) {
                if (formatHint && !formatHint.hidden) positionHint(input, formatHint);
                return;
            }
            if (panelBundle) positionPanel(input, popup);
            else positionList(input, popup);
        }, true);

        document.addEventListener('click', function (e) {
            if (!input.contains(e.target) && !popup.contains(e.target) && (!formatHint || !formatHint.contains(e.target))) {
                hide();
            }
        });
    }

    function init() {
        // Clean up previously created popup artifacts (e.g. after hot-reload/script re-exec)
        // so we do not stack duplicate "Saved info" panels.
        document.querySelectorAll('.bse-autosuggest-panel, .bse-autosuggest-format-hint, .bse-autosuggest-list[data-bse-autosuggest-floating="true"]')
            .forEach(function (el) { el.remove(); });
        document.querySelectorAll('input[data-bse-autosuggest-bound="true"]')
            .forEach(function (input) { input.removeAttribute('data-bse-autosuggest-bound'); });

        void preloadField('rbse');
        void preloadField('cphh');

        var inputs = Array.from(document.querySelectorAll('input'));
        inputs.filter(isEligible).forEach(attachAutosuggest);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
