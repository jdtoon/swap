// Close modal on Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        // Close DaisyUI modal
        const modal = document.querySelector('.modal.modal-open');
        if (modal) {
            modal.classList.remove('modal-open');
        }
    }
});
