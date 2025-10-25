// assignUsers.js
window.AssignUsers = window.AssignUsers = {
    init: function (itemId) {
        document.getElementById(`assign-users-modal-${itemId}`).showModal();
        this.setupEventListeners(itemId);
    },

    setupEventListeners: function (itemId) {
        // Add click outside handler
        document.addEventListener('click', (event) => this.handleClickOutside(event, itemId));

        // Prevent modal close when clicking dropdown
        const dropdown = document.getElementById(`user-dropdown-${itemId}`);
        if (dropdown) {
            dropdown.addEventListener('click', function (event) {
                event.stopPropagation();
            });
        }
    },

    closeModal: function (id) {
        document.getElementById(`assign-users-modal-${id}`).close();
        document.getElementById(`assign-users-modal-${id}`).remove();
        document.getElementById('modal-container').innerHTML = '';
    },

    showDropdown: function (itemId) {
        const dropdown = document.getElementById(`user-dropdown-${itemId}`);
        if (dropdown) {
            dropdown.classList.remove('hidden');
        }
    },

    filterUsers: function (itemId) {
        const searchInput = document.getElementById(`user-search-${itemId}`);
        const filter = searchInput?.value.toLowerCase() || '';
        const dropdown = document.getElementById(`user-dropdown-${itemId}`);
        const options = dropdown?.getElementsByClassName('user-option') || [];

        if (filter.length === 0) {
            dropdown?.classList.add('hidden');
            return;
        }

        dropdown?.classList.remove('hidden');

        for (let option of options) {
            const userName = option.dataset.userName.toLowerCase();
            option.style.display = userName.includes(filter) ? "" : "none";
        }
    },

    toggleUserSelection: function (itemId, userId, userName) {
        const checkbox = document.getElementById(`user-checkbox-${itemId}-${userId}`);
        const selectedUsersContainer = document.getElementById(`selected-users-${itemId}`);
        const pillId = `pill-${itemId}-${userId}`;

        if (!checkbox || !selectedUsersContainer) return;

        checkbox.checked = !checkbox.checked;

        if (checkbox.checked) {
            if (!document.getElementById(pillId)) {
                const pill = document.createElement('div');
                pill.id = pillId;
                pill.className = 'badge badge-primary gap-2';
                pill.innerHTML = `
                    <span>${userName}</span>
                    <button type="button" onclick="AssignUsers.toggleUserSelection(${itemId}, '${userId}', '${userName}')" class="btn btn-ghost btn-xs px-1">×</button>
                `;
                selectedUsersContainer.appendChild(pill);
            }
        } else {
            const existingPill = document.getElementById(pillId);
            if (existingPill) {
                existingPill.remove();
            }
        }
    },

    handleClickOutside: function (event, itemId) {
        const dropdown = document.getElementById(`user-dropdown-${itemId}`);
        const searchInput = document.getElementById(`user-search-${itemId}`);

        if (dropdown && searchInput) {
            if (!dropdown.contains(event.target) && !searchInput.contains(event.target)) {
                dropdown.classList.add('hidden');
            }
        }
    }
};