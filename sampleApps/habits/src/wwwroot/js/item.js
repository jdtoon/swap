window.initiateItemList =
    window.initiateItemList ||
    function (url, taskListId) {
        var el = document.getElementById("items-sortable");
        var sortable = Sortable.create(el, {
            animation: 150,
            delay: 100,
            delayOnTouchOnly: true,
            onEnd: function (evt) {
                const itemElement = evt.item;
                const itemId = parseInt(itemElement.getAttribute("data-id"));
                const newIndex = evt.newIndex;

                htmx
                    .ajax("POST", url, {
                        values: {
                            id: itemId.toString(),
                            newPosition: (newIndex + 1).toString(),
                            taskListId: taskListId,
                        },
                        target: "#items-sortable",
                        swap: "none",
                    })
                    .then(() => {
                        itemElement.classList.add("opacity-75");
                        setTimeout(() => itemElement.classList.remove("opacity-75"), 300);

                        Array.from(el.children).forEach((item, index) => {
                            item.setAttribute("data-order", index + 1);
                        });
                    })
                    .catch((error) => {
                        const items = Array.from(el.children);
                        items.sort((a, b) => {
                            return (
                                parseInt(a.getAttribute("data-order")) -
                                parseInt(b.getAttribute("data-order"))
                            );
                        });
                        items.forEach((item) => el.appendChild(item));
                    });
            },
        });
    };

window.closeItemModal =
    window.closeItemModal ||
    function (id) {
        document.getElementById(`edit-modal-${id}`).close();
        document.getElementById(`edit-modal-${id}`).remove();
    };

window.handleAfterSettle =
    window.handleAfterSettle ||
    function (evt) {
        updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
    };

document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);

window.setupItemEventListeners =
    window.setupItemEventListeners ||
    function () {
        const container = document.getElementById('items-sortable');
        if (!container) return;

        const deleteButtonContainer = document.getElementById('delete-checked-container');
        const deleteButton = document.getElementById('delete-checked-btn');
        const checkboxes = container.querySelectorAll('input[type="checkbox"]');

        const updateButtonVisibility = () => {
            const anyChecked = Array.from(checkboxes).some(cb => cb.checked);
            if (deleteButtonContainer) {
                deleteButtonContainer.style.display = anyChecked ? 'flex' : 'none';
            }
        };

        checkboxes.forEach(checkbox => {
            if (!checkbox.dataset.listenerAdded) {
                checkbox.addEventListener('change', updateButtonVisibility);
                checkbox.dataset.listenerAdded = 'true';
            }
        });

        if (deleteButton && !deleteButton.dataset.listenerAdded) {
            deleteButton.addEventListener('click', () => {
                const checkedIds = Array.from(container.querySelectorAll('input[type="checkbox"]:checked'))
                    .map(cb => parseInt(cb.closest('.sortable-item').getAttribute('data-id')));

                if (checkedIds.length > 0 && confirm('Are you sure you want to delete the selected items?')) {
                    const url = `/Item/DeleteItems?${checkedIds.map(id => `ids=${id}`).join('&')}`;

                    htmx.ajax('DELETE', url, {
                        target: "#primary-task-container",
                        swap: 'none'
                    }).then(() => {
                        checkedIds.forEach(id => {
                            const itemToRemove = document.getElementById(`item-${id}`);
                            if (itemToRemove) {
                                itemToRemove.remove();
                            }
                        });
                        updateButtonVisibility();
                    }).catch(() => {
                        alert('An error occurred while deleting items.');
                    });
                }
            });
            deleteButton.dataset.listenerAdded = 'true';
        }

        updateButtonVisibility();
    };

document.removeEventListener("htmx:afterSettle", window.setupItemEventListeners);
document.addEventListener("htmx:afterSettle", window.setupItemEventListeners);