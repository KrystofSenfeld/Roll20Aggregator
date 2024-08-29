function closeDialog(dialogId) {
    const modalElement = document.getElementById(dialogId);
    const modal = bootstrap.Modal.getInstance(modalElement);
    modal.hide();
}

function initializeTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
}