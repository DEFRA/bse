// Progressive enhancement for /Admin/Users: selecting, adding and editing a row happens
// entirely client-side (no page reload). Only Save still performs a real form submit,
// since persisting a change always requires a server round-trip. If this script fails to
// load, the existing server-rendered links/handlers still work via full page navigation.
(function () {
    'use strict';

    var table = document.querySelector('.bse-user-maintenance-table');
    if (!table) return;

    var form = table.closest('form');
    var tbody = table.querySelector('tbody');
    var newBtn = form.querySelector('[data-action="start-new"]');
    var editBtn = form.querySelector('[data-action="start-edit"]');
    var saveBtn = form.querySelector('[data-action="save"]');
    var cancelBtn = form.querySelector('[data-action="cancel"]');
    var groupsDataEl = document.getElementById('bse-user-groups-data');
    var groups = groupsDataEl ? JSON.parse(groupsDataEl.textContent) : [];

    var selectedRow = null;
    var editingRow = null;
    var originalRowHtml = null;
    var isNewRow = false;
    // True when the edit row came from the server (e.g. re-rendered after a validation error)
    // rather than being built here, so we have no original markup to restore on cancel.
    var isServerRendered = false;

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
        table.querySelectorAll('tr.bse-row-selected').forEach(function (r) { r.classList.remove('bse-row-selected'); });
        row.classList.add('bse-row-selected');
        selectedRow = row;
        var hiddenSelectedId = form.querySelector('[name="SelectedUserId"]');
        if (hiddenSelectedId) hiddenSelectedId.value = row.dataset.userId;
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

        tbody.appendChild(row);
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
    table.addEventListener('click', function (e) {
        var link = e.target.closest('.bse-row-select-link');
        if (!link) return;
        e.preventDefault();
        if (editingRow) return;
        selectRow(link.closest('tr'));
    });

    if (newBtn) {
        newBtn.addEventListener('click', function (e) {
            // A new record always belongs at the end of the last page. If we're not there, or the
            // last page is already full, fall through to the server handler so it navigates for us.
            var onLastPage = table.dataset.pageNumber === table.dataset.totalPages;
            if (!onLastPage || table.dataset.pageSizeFull === 'true') return;
            e.preventDefault();
            startNew();
        });
    }

    if (editBtn) {
        editBtn.addEventListener('click', function (e) {
            e.preventDefault();
            startEdit();
        });
    }

    form.addEventListener('click', function (e) {
        var cancel = e.target.closest('[data-action="cancel"]');
        if (!cancel) return;
        if (isServerRendered) return; // let the server Cancel handler reset the page state
        e.preventDefault();
        cancelEdit();
    });

    // Adopt whatever state the server rendered — after a validation failure the edit row comes
    // back from the server, and without this the buttons would reset to "nothing selected".
    var serverEditRow = tbody.querySelector('tr.bse-row-editing');
    if (serverEditRow) {
        editingRow = serverEditRow;
        isServerRendered = true;
        isNewRow = !serverEditRow.dataset.userId;
        selectedRow = isNewRow ? null : serverEditRow;
    } else {
        selectedRow = tbody.querySelector('tr.bse-row-selected');
    }

    // Save keeps its native type="submit" behaviour — a real postback is required to persist.
    refreshButtonState();
})();
