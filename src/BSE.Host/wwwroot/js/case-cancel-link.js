// Shared Cancel-link behaviour for the Case/*.cshtml edit tabs (Feeds, Clinical, Edit, Farm,
// Relations) — extracted from identical inline <script> IIFEs duplicated across those pages
// (SonarCloud "Duplicated Lines (%) on New Code"). Bab/Vla/DefraEdit have their own, materially
// different dirty-tracking logic and are intentionally not wired through this helper.
window.BSE = window.BSE || {};

// Legacy's Cancel is a postback that captures the current form before checking for changes, so
// edits typed on this tab but never staged anywhere must count as unsaved too. hasUnsavedChanges
// is the page's server-rendered Model.HasUnsavedChanges (cross-tab staged state) at page-load time.
window.BSE.initCaseCancelLink = function (formId, hasUnsavedChanges) {
    var cancelForm = document.getElementById(formId);
    var formDirty = false;
    if (cancelForm) {
        cancelForm.addEventListener('input', function () { formDirty = true; });
        cancelForm.addEventListener('change', function () { formDirty = true; });
    }

    document.querySelectorAll('.bse-cancel-link').forEach(function (lnk) {
        lnk.addEventListener('click', function (e) {
            e.preventDefault();
            var hasUnsavedHere = formDirty || hasUnsavedChanges;
            // Legacy's dirty-check spans the whole shared session (every tab), not just this page.
            (hasUnsavedHere ? Promise.resolve(true) : (window.hasUnsavedChangesAcrossTabs ? window.hasUnsavedChangesAcrossTabs() : Promise.resolve(false)))
                .then(function (hasUnsaved) {
                    if (!hasUnsaved || window.confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
                        window.location.href = lnk.href;
                    }
                });
        });
    });
};
