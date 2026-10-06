// Progressive enhancement for /Admin/Users.
//
// The grid refreshes in place: selecting, adding and editing happen purely client-side, while
// paging, sorting and saving fetch the page and swap only the grid container, so the browser
// never performs a full navigation. Without JavaScript every one of these still works as a
// normal link or form post.
(function () {
    'use strict';

    var CONTAINER_ID = 'bse-users-grid';
    var container = document.getElementById(CONTAINER_ID);
    if (!container) return;

    var selectedRow = null;
    var editingRow = null;
    var originalRowHtml = null;
    var isNewRow = false;
    // True when the edit row came from the server (e.g. re-rendered after a validation error)
    // rather than being built here, so we have no original markup to restore on cancel.
    var isServerRendered = false;
    var groups = [];

    function table() { return container.querySelector('.bse-user-maintenance-table'); }
    function form() { return container.querySelector('form'); }
    function button(action) { return container.querySelector('[data-action="' + action + '"]'); }

    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function groupOptionsHtml(selectedId) {
        return groups.map(function (g) {
            var selected = String(g.id) === String(selectedId) ? ' selected' : '';
            return '<option value="' + g.id + '"' + selected + '>' + escapeHtml(g.name) + '</option>';
        }).join('');
    }

    function refreshButtonState() {
        var hasSelection = !!selectedRow;
        var editing = !!editingRow;
        var newBtn = button('start-new'), editBtn = button('start-edit');
        var saveBtn = button('save'), cancelBtn = button('cancel');
        if (newBtn) newBtn.disabled = editing;
        if (editBtn) editBtn.disabled = editing || !hasSelection;
        if (saveBtn) saveBtn.disabled = !editing;
        if (cancelBtn) cancelBtn.disabled = !editing;
    }

    function editCellsHtml(data) {
        return ''
            + '<td class="govuk-table__cell">'
            + '<input type="text" id="edit-user-name" name="EditUserName" class="govuk-input" value="' + escapeHtml(data.userName) + '" />'
            + '</td>'
            + '<td class="govuk-table__cell">'
            + '<input type="text" id="edit-email" name="EditEmail" class="govuk-input" value="' + escapeHtml(data.email) + '" />'
            + '</td>'
            + '<td class="govuk-table__cell">'
            + '<select id="edit-user-group-id" name="EditUserGroupId" class="govuk-select">' + groupOptionsHtml(data.groupId) + '</select>'
            + '</td>'
            + '<td class="govuk-table__cell">'
            + '<div class="govuk-checkboxes govuk-checkboxes--small bse-inline-checkbox">'
            + '<div class="govuk-checkboxes__item">'
            + '<input type="checkbox" id="EditIsActive" name="EditIsActive" value="true" class="govuk-checkboxes__input"' + (data.isActive ? ' checked' : '') + ' />'
            + '<label class="govuk-label govuk-checkboxes__label" for="EditIsActive"><span class="govuk-visually-hidden">Active</span></label>'
            // Must stay after the label: GOV.UK draws the tick via "input:checked + label::after".
            + '<input type="hidden" name="EditIsActive" value="false" />'
            + '</div></div>'
            + '</td>';
    }

    function selectRow(row) {
        if (editingRow) return;
        container.querySelectorAll('tr.bse-row-selected').forEach(function (r) { r.classList.remove('bse-row-selected'); });
        row.classList.add('bse-row-selected');
        selectedRow = row;
        var hidden = form() && form().querySelector('[name="SelectedUserId"]');
        if (hidden) hidden.value = row.dataset.userId;
        refreshButtonState();
    }

    function startEdit() {
        if (!selectedRow || editingRow) return;
        originalRowHtml = selectedRow.innerHTML;
        isNewRow = false;

        var data = {
            userId: selectedRow.dataset.userId,
            userName: selectedRow.dataset.userName,
            email: selectedRow.dataset.email,
            groupId: selectedRow.dataset.groupId,
            ntLogin: selectedRow.dataset.ntLogin,
            upn: selectedRow.dataset.upn,
            isActive: selectedRow.dataset.isActive === 'true'
        };

        var selectCell = selectedRow.querySelector('.bse-row-select-cell');
        selectedRow.classList.add('bse-row-editing');
        selectedRow.classList.remove('bse-row-selected');
        selectedRow.innerHTML = '';
        selectedRow.appendChild(selectCell);
        selectedRow.insertAdjacentHTML('beforeend',
            '<input type="hidden" name="EditUserId" value="' + escapeHtml(data.userId) + '" />'
            + '<input type="hidden" name="EditUpn" value="' + escapeHtml(data.upn) + '" />'
            + '<input type="hidden" name="EditNTLogin" value="' + escapeHtml(data.ntLogin) + '" />'
            + editCellsHtml(data));

        editingRow = selectedRow;
        refreshButtonState();
        var nameInput = document.getElementById('edit-user-name');
        if (nameInput) nameInput.focus();
    }

    function startNew() {
        if (editingRow) return;
        isNewRow = true;

        var row = document.createElement('tr');
        row.className = 'govuk-table__row bse-row-editing';
        row.innerHTML = '<td class="govuk-table__cell bse-row-select-cell"></td>'
            + '<input type="hidden" name="EditUserId" value="0" />'
            + '<input type="hidden" name="EditUpn" value="" />'
            + editCellsHtml({ userName: '', email: '', groupId: '', isActive: true });

        table().querySelector('tbody').appendChild(row);
        editingRow = row;
        refreshButtonState();
        var nameInput = document.getElementById('edit-user-name');
        if (nameInput) nameInput.focus();
    }

    function cancelEdit() {
        if (!editingRow) return;
        if (isNewRow) {
            editingRow.remove();
        } else {
            editingRow.innerHTML = originalRowHtml;
            editingRow.classList.remove('bse-row-editing');
            editingRow.classList.add('bse-row-selected');
        }
        editingRow = null;
        originalRowHtml = null;
        isNewRow = false;
        refreshButtonState();
    }

    // Replaces just the grid markup with the equivalent region from a freshly rendered page.
    function swapGrid(html, url) {
        var parsed = new DOMParser().parseFromString(html, 'text/html');
        var fresh = parsed.getElementById(CONTAINER_ID);
        if (!fresh) { window.location.assign(url); return; }

        container.innerHTML = fresh.innerHTML;
        if (url) history.pushState({}, '', url);
        adoptServerState();
    }

    function loadGrid(url) {
        container.setAttribute('aria-busy', 'true');
        return fetch(url, { headers: { 'X-Requested-With': 'fetch' }, credentials: 'same-origin' })
            .then(function (r) { return r.text().then(function (html) { return { html: html, url: r.url || url }; }); })
            .then(function (res) { swapGrid(res.html, res.url); })
            .catch(function () { window.location.assign(url); })
            .finally(function () { container.removeAttribute('aria-busy'); });
    }

    function submitGrid(submitter) {
        var f = form();
        if (!f) return;
        var data = new FormData(f);
        if (submitter && submitter.name) data.append(submitter.name, submitter.value || '');
        var action = (submitter && submitter.getAttribute('formaction')) || f.getAttribute('action') || window.location.href;

        container.setAttribute('aria-busy', 'true');
        return fetch(action, {
            method: 'POST', body: data,
            headers: { 'X-Requested-With': 'fetch' },
            credentials: 'same-origin', redirect: 'follow'
        })
            .then(function (r) { return r.text().then(function (html) { return { html: html, url: r.url }; }); })
            .then(function (res) { swapGrid(res.html, res.url); })
            .catch(function () { f.submit(); })
            .finally(function () { container.removeAttribute('aria-busy'); });
    }

    // Adopt whatever state the server rendered — after a validation failure the edit row comes
    // back from the server, and without this the buttons would reset to "nothing selected".
    function adoptServerState() {
        selectedRow = null;
        editingRow = null;
        originalRowHtml = null;
        isNewRow = false;
        isServerRendered = false;

        var dataEl = document.getElementById('bse-user-groups-data');
        if (dataEl) { try { groups = JSON.parse(dataEl.textContent); } catch (e) { groups = []; } }

        var t = table();
        if (!t) return;
        var serverEditRow = t.querySelector('tr.bse-row-editing');
        if (serverEditRow) {
            editingRow = serverEditRow;
            isServerRendered = true;
            isNewRow = !serverEditRow.dataset.userId;
            selectedRow = isNewRow ? null : serverEditRow;
        } else {
            selectedRow = t.querySelector('tr.bse-row-selected');
        }
        refreshButtonState();
    }

    function isOnLastPageWithRoom() {
        var t = table();
        return !!t && t.dataset.pageNumber === t.dataset.totalPages && t.dataset.pageSizeFull !== 'true';
    }

    // Delegated so the handlers survive each grid swap.
    container.addEventListener('click', function (e) {
        var selectLink = e.target.closest('.bse-row-select-link');
        if (selectLink) {
            e.preventDefault();
            if (!editingRow) selectRow(selectLink.closest('tr'));
            return;
        }

        var action = e.target.closest('[data-action]');
        if (action) {
            var name = action.dataset.action;
            e.preventDefault();
            if (name === 'start-new') {
                if (isOnLastPageWithRoom()) startNew(); else submitGrid(action);
            } else if (name === 'start-edit') {
                startEdit();
            } else if (name === 'cancel') {
                if (isServerRendered) submitGrid(action); else cancelEdit();
            } else if (name === 'save') {
                submitGrid(action);
            }
            return;
        }

        // Paging and column sorting only ever change the grid, so refresh it in place.
        var link = e.target.closest('.govuk-pagination__link, .bse-sortable-table__button');
        if (link && link.getAttribute('href')) {
            e.preventDefault();
            loadGrid(new URL(link.getAttribute('href'), window.location.href).toString());
        }
    });

    container.addEventListener('submit', function (e) {
        e.preventDefault();
        submitGrid(e.submitter);
    });

    window.addEventListener('popstate', function () { loadGrid(window.location.href); });

    adoptServerState();
})();
