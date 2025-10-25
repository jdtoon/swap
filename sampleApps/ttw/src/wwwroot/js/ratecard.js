// State management (ensure initialization if not already present)
window.rateCardState = window.rateCardState || {
    cardId: 1,    // Next available ID to assign to a NEW card
    cardList: [], // List of active card IDs (using data-card-id)
    targetCardForAddRow: null, // Card to add a row to (if applicable)
};

// --- Helper Functions ---

// Get room type options stored in the card
function getRoomTypeOptions(card) {
    const optionsScript = card.querySelector('.js-room-type-options');
    if (!optionsScript) {
        console.warn("Room type options script tag not found in card:", card);
        return [];
    }
    try {
        const optionsData = JSON.parse(optionsScript.textContent);
        // Ensure it's an array of objects with Text and Value properties (like SelectListItem)
        if (Array.isArray(optionsData)) {
            if (optionsData.length === 0 || (typeof optionsData[0] === 'object' && optionsData[0] !== null && optionsData[0].hasOwnProperty('Text') && optionsData[0].hasOwnProperty('Value'))) {
                return optionsData; // It's an array, empty or looks like SelectListItem format
            }
        }
        console.warn("Room type options data not in expected SelectList format (Array of { Text, Value }).", optionsData);
        return []; // Return empty array if parsing fails or format is wrong
    } catch (e) {
        console.error("Error parsing room type options JSON:", e, optionsScript?.textContent);
        return [];
    }
}

// Update row indices (data-row-index) after add/remove
function updateRowIndices(tbody) {
    let rowIndex = 0;
    tbody.querySelectorAll('.js-rate-row').forEach(row => {
        // Only increment index for the primary row of a pair (WD or Flat)
        if (!row.classList.contains('js-we-row')) {
            // Update the hidden enum input first if it exists right before this row
            const enumInput = row.previousElementSibling;
            if (enumInput && enumInput.classList.contains('js-rate-type-enum')) {
                enumInput.dataset.rowIndex = rowIndex;
            }

            // Update the row itself
            row.dataset.rowIndex = rowIndex;
            // Update indices on all relevant elements within this row
            row.querySelectorAll('[data-row-index]').forEach(el => el.dataset.rowIndex = rowIndex);

            rowIndex++;
        } else {
            // WE row should share the index of its WD sibling
            const wdIndex = rowIndex - 1; // Use the same index as the preceding WD row
            row.dataset.rowIndex = wdIndex;
            row.querySelectorAll('[data-row-index]').forEach(el => el.dataset.rowIndex = wdIndex);
        }
    });
    return rowIndex; // Return the new total number of date ranges (primary rows)
}

// Update room indices (data-room-index) after add/remove
function updateRoomIndices(card) {
    let roomIndex = 0;
    // Update header indices first (only room headers)
    card.querySelectorAll('.js-header-row-main .js-room-header').forEach(th => {
        th.querySelectorAll('[data-room-index]').forEach(el => el.dataset.roomIndex = roomIndex);
        // Also update the empty alignment TH in the sub-header, if found by index
        const subHeaderCell = card.querySelectorAll(`.js-header-row-sub th`)[roomIndex + 4]; // Index 4 = after Remove, From, To, Type
        if (subHeaderCell) subHeaderCell.dataset.roomIndex = roomIndex; // Add index if missing

        roomIndex++;
    });

    // Update body cell indices (only room data cells)
    card.querySelectorAll('.js-rate-table-body tr').forEach(tr => {
        let currentRoomIndex = 0;
        // Select only TDs that should have a room index
        tr.querySelectorAll('td[data-room-index]').forEach(td => {
            td.dataset.roomIndex = currentRoomIndex;
            // Update elements within the TD as well
            td.querySelectorAll('[data-room-index]').forEach(el => el.dataset.roomIndex = currentRoomIndex);
            currentRoomIndex++;
        });
    });
    return roomIndex; // Return the new total number of rooms
}

// Update meal rowspan for global meal notes
function updateMealRowspan(card) {
    const mealCell = card.querySelector('.js-global-meal-cell');
    if (!mealCell) return; // Only applicable if not mealPerRow

    const tbody = card.querySelector('.js-rate-table-body');
    let flatRateCount = 0;
    let weekendRateCount = 0; // Count pairs

    tbody.querySelectorAll('.js-rate-row').forEach(row => {
        // Count primary rows only (WD or Flat)
        if (!row.classList.contains('js-we-row')) {
            const enumInput = row.previousElementSibling;
            const rateType = (enumInput && enumInput.classList.contains('js-rate-type-enum')) ? enumInput.value : '1'; // Default to flat if enum not found

            if (rateType === '2' || row.classList.contains('js-wd-row')) { // Weekend/Weekday pair primary row
                weekendRateCount++;
            } else { // Flat rate row
                flatRateCount++;
            }
        }
    });

    const newRowSpan = (weekendRateCount * 2) + flatRateCount;
    mealCell.setAttribute('rowspan', newRowSpan > 0 ? newRowSpan : 1); // Ensure rowspan is at least 1
}

// Update colspan on the footer comment cell
function updateCommentColspan(card, roomCount) {
    const commentCell = card.querySelector('tfoot .js-comment-cell');
    if (commentCell) {
        // Colspan = RemoveBtn(1) + From(1) + To(1) + Type(1) + RoomCounts
        commentCell.setAttribute('colspan', 1 + 1 + 1 + 1 + roomCount);
    }
    // Also update the meal spacer cell colspan if it exists (when not meal per row)
    const mealSpacer = card.querySelector('tfoot .js-comment-meal-spacer');
    if (mealSpacer) {
        // Should always be 1? Or match meal header colspan? Assume 1 for now.
        mealSpacer.setAttribute('colspan', 1);
    }
}

// --- NEW: Show Modal and Store Context ---
window.promptForRowType = window.promptForRowType || function (addButton) {
    const card = addButton.closest('.rate-card');
    if (!card) {
        console.error("Could not find target card for adding row.");
        return;
    }
    // Store the card element reference (or its ID) so the modal knows where to add the row
    window.rateCardState.targetCardForAddRow = card;
    // Or: document.getElementById('addRowTypeModal').dataset.targetCardId = card.dataset.cardId;

    showModal('addRowTypeModal');
};

// --- NEW: Function called by Modal Buttons ---
window.doAddRowWithType = window.doAddRowWithType || function (selectionType) {
    const targetCard = window.rateCardState.targetCardForAddRow;
    // Or: const targetCardId = document.getElementById('addRowTypeModal').dataset.targetCardId;
    // const targetCard = document.querySelector(`.rate-card[data-card-id="${targetCardId}"]`);

    if (!targetCard) {
        console.error("Target card reference lost.");
        alert("Error: Could not find the card to add the row to. Please try again.");
        closeModal('addRowTypeModal');
        return;
    }

    // Call the refactored addRow function
    addRow(targetCard, selectionType);

    // Clean up and close modal
    window.rateCardState.targetCardForAddRow = null;
    // Or: delete document.getElementById('addRowTypeModal').dataset.targetCardId;
    closeModal('addRowTypeModal');
};

// --- Add/Remove Row/Column Functions ---

window.addRow = window.addRow || function (card, selectionType) { // Now accepts card element and type
    // Removed button parameter processing, card is passed directly

    if (!card) { console.error("No card provided to addRow"); return; }
    const tbody = card.querySelector('.js-rate-table-body');
    if (!tbody) { console.error("Table body not found in card", card); return; }

    const roomCount = parseInt(card.querySelector('.js-no-of-rooms')?.value || '0', 10);
    const isMealPerRow = card.querySelector('.js-meal-per-row-value')?.value?.toLowerCase() === 'true';
    const currentRowCount = parseInt(card.querySelector('.js-no-of-rows')?.value || '0', 10);
    const newRowIndex = currentRowCount; // 0-based index for the new row

    // --- Create and Insert hidden Enum input ---
    const enumInput = document.createElement('input');
    enumInput.type = 'hidden';
    enumInput.classList.add('js-rate-type-enum');
    enumInput.dataset.rowIndex = newRowIndex;
    enumInput.value = selectionType; // Use the chosen type
    // Insert before where the first new TR will go (append to tbody is simplest)
    tbody.appendChild(enumInput);

    // --- Build Row(s) based on selectionType ---
    if (selectionType === '1') {
        // --- Create Flat Rate Row ---
        const newRow = document.createElement('tr');
        newRow.classList.add('js-rate-row');
        newRow.dataset.rowIndex = newRowIndex;

        let rowHTML = `<td><button type="button" class="btn btn-xs btn-ghost text-error p-0" onclick="removeRow(this)" title="Remove Date Range">X</button></td>`;
        rowHTML += `<td><input type="date" class="input input-xs input-bordered w-full js-from-date" data-row-index="${newRowIndex}" /></td>`;
        rowHTML += `<td><input type="date" class="input input-xs input-bordered w-full js-to-date" data-row-index="${newRowIndex}" /></td>`;
        rowHTML += `<td>Flat</td>`;
        for (let i = 0; i < roomCount; i++) {
            rowHTML += `<td data-room-index="${i}"><input type="number" class="input input-xs input-bordered w-full text-center js-rate1" data-row-index="${newRowIndex}" data-room-index="${i}" /></td>`;
        }
        if (isMealPerRow) {
            rowHTML += `<td class="js-meal-cell" data-row-index="${newRowIndex}"><input type="text" class="input input-xs input-bordered w-full js-meal-note-row" data-row-index="${newRowIndex}" placeholder="...meal notes..." /></td>`;
        }
        newRow.innerHTML = rowHTML;
        tbody.appendChild(newRow);

    } else if (selectionType === '2') {
        // --- Create Weekend/Weekday Row Pair ---
        const wdRow = document.createElement('tr');
        wdRow.classList.add('js-rate-row', 'js-wd-row');
        wdRow.dataset.rowIndex = newRowIndex;

        const weRow = document.createElement('tr');
        weRow.classList.add('js-rate-row', 'js-we-row');
        weRow.dataset.rowIndex = newRowIndex;

        // W/D Row HTML
        let wdHTML = `<td rowspan="2"><button type="button" class="btn btn-xs btn-ghost text-error p-0" onclick="removeRow(this)" title="Remove Date Range">X</button></td>`; // Rowspan on remove button cell
        wdHTML += `<td rowspan="2"><input type="date" class="input input-xs input-bordered w-full js-from-date" data-row-index="${newRowIndex}" /></td>`; // Rowspan on From
        wdHTML += `<td rowspan="2"><input type="date" class="input input-xs input-bordered w-full js-to-date" data-row-index="${newRowIndex}" /></td>`; // Rowspan on To
        wdHTML += `<td>W/D</td>`; // Type
        for (let i = 0; i < roomCount; i++) { // Rate 1 inputs
            wdHTML += `<td data-room-index="${i}"><input type="number" class="input input-xs input-bordered w-full text-center js-rate1" data-row-index="${newRowIndex}" data-room-index="${i}" /></td>`;
        }
        if (isMealPerRow) { // Meal cell per row
            wdHTML += `<td class="js-meal-cell" rowspan="2" data-row-index="${newRowIndex}"><input type="text" class="input input-xs input-bordered w-full js-meal-note-row" data-row-index="${newRowIndex}" placeholder="...meal notes..." /></td>`; // Rowspan on meal cell
        }
        wdRow.innerHTML = wdHTML;

        // W/E Row HTML
        let weHTML = `<td>W/E</td>`; // Type
        for (let i = 0; i < roomCount; i++) { // Rate 2 inputs
            weHTML += `<td data-room-index="${i}"><input type="number" class="input input-xs input-bordered w-full text-center js-rate2" data-row-index="${newRowIndex}" data-room-index="${i}" /></td>`;
        }
        // Meal cell is spanned from WD row if isMealPerRow = true
        weRow.innerHTML = weHTML;

        tbody.appendChild(wdRow);
        tbody.appendChild(weRow);
    } else {
        console.error("Invalid selectionType passed to addRow:", selectionType);
        return; // Don't proceed if type is invalid
    }

    // --- Update Counts, Indices, and Spans ---
    // Increment count regardless of type (it counts date *ranges*)
    const newRowCount = updateRowIndices(tbody); // Re-index rows AND get the new total count
    card.querySelector('.js-no-of-rows').value = newRowCount; // Update hidden input

    if (!isMealPerRow) {
        updateMealRowspan(card); // Update global meal cell rowspan if needed
    }
};

window.removeRow = window.removeRow || function (button) {
    if (!confirm("Are you sure you want to remove this date range?")) {
        return;
    }
    const card = button.closest('.rate-card');
    if (!card) return;
    const tbody = card.querySelector('.js-rate-table-body');
    if (!tbody) return;
    // The row containing the button (could be Flat or W/D row)
    const rowToRemove = button.closest('tr');
    if (!rowToRemove) return;

    const isMealPerRow = card.querySelector('.js-meal-per-row-value')?.value?.toLowerCase() === 'true';

    // Find associated hidden enum input (should be directly before the primary row)
    const enumInput = rowToRemove.previousElementSibling;
    const enumToRemove = (enumInput && enumInput.classList.contains('js-rate-type-enum')) ? enumInput : null;

    let rowRemoved = false;
    // Check if it's a W/D row (has rowspan=2 on first cell)
    const firstCell = rowToRemove.querySelector('td'); // Get first TD
    if (firstCell && firstCell.getAttribute('rowspan') === '2') {
        // It's a W/D row, find and remove the next sibling (W/E row) too
        const weRow = rowToRemove.nextElementSibling;
        if (weRow && weRow.classList.contains('js-we-row') && weRow.dataset.rowIndex === rowToRemove.dataset.rowIndex) {
            weRow.remove();
        } else {
            console.warn("Could not find matching W/E row to remove for row index:", rowToRemove.dataset.rowIndex);
        }
        // Now remove the W/D row itself
        rowToRemove.remove();
        rowRemoved = true;
    } else if (!rowToRemove.classList.contains('js-we-row')) {
        // It's a single row (Flat Rate) - ensure it's not the W/E part accidentally
        rowToRemove.remove();
        rowRemoved = true;
    } else {
        // Should not happen if remove button is only on primary row
        console.warn("Attempted to remove a W/E row directly.");
        return;
    }

    // Remove the associated enum input if found and a row was removed
    if (enumToRemove && rowRemoved) {
        enumToRemove.remove();
    }

    // --- Update Counts, Indices, and Spans if a row was removed ---
    if (rowRemoved) {
        const newRowCount = updateRowIndices(tbody); // Re-index rows and get count
        card.querySelector('.js-no-of-rows').value = newRowCount; // Update hidden count

        if (!isMealPerRow) {
            updateMealRowspan(card); // Update rowspan for global meal cell
        }
    }
};

window.addColumn = window.addColumn || function (button) {
    const card = button.closest('.rate-card');
    if (!card) return;
    const roomOptions = getRoomTypeOptions(card);
    const currentRoomCount = parseInt(card.querySelector('.js-no-of-rooms')?.value || '0', 10);
    const newRoomIndex = currentRoomCount; // 0-based index for the new column

    // --- Add Header Cell ---
    const headerRow = card.querySelector('.js-header-row-main');
    const subHeaderRow = card.querySelector('.js-header-row-sub');
    if (!headerRow || !subHeaderRow) {
        console.error("Header rows not found"); return;
    }

    const newTh = document.createElement('th');
    newTh.classList.add('js-room-header');
    // Create select dropdown
    const select = document.createElement('select');
    select.classList.add('select', 'select-xs', 'select-bordered', 'w-full', 'js-room-type-select');
    select.dataset.roomIndex = newRoomIndex;
    select.innerHTML = '<option value="">Select Room...</option>';
    roomOptions.forEach(opt => {
        select.innerHTML += `<option value="${opt.Value}">${opt.Text}</option>`;
    });
    // Create remove button
    const removeBtn = document.createElement('button');
    removeBtn.type = 'button';
    removeBtn.classList.add('btn', 'btn-xs', 'btn-ghost', 'text-error', 'p-0', 'ml-1');
    removeBtn.title = 'Remove Room Type';
    removeBtn.textContent = 'X';
    removeBtn.onclick = function () { removeColumn(this); };

    newTh.appendChild(select);
    newTh.appendChild(removeBtn);

    // Add empty TH for alignment in sub-header
    const newSubTh = document.createElement('th');
    newSubTh.dataset.roomIndex = newRoomIndex; // Add index here

    // Insert before the Meal Header if it exists
    const mealHeader = headerRow.querySelector('.js-meal-header');
    const subHeaderMealSpacer = subHeaderRow.cells[subHeaderRow.cells.length - 1]; // Assuming meal spacer is last

    if (mealHeader) {
        headerRow.insertBefore(newTh, mealHeader);
        if (subHeaderMealSpacer) { // Insert before the last cell in sub-header
            subHeaderRow.insertBefore(newSubTh, subHeaderMealSpacer);
        } else {
            subHeaderRow.appendChild(newSubTh); // Append if no spacer? Should exist.
        }
    } else {
        // If no meal header (shouldn't happen with current HTML), just append
        headerRow.appendChild(newTh);
        subHeaderRow.appendChild(newSubTh);
    }

    // --- Add Body Cells ---
    const tbody = card.querySelector('.js-rate-table-body');
    if (!tbody) { console.error("Table body not found"); return; }
    tbody.querySelectorAll('.js-rate-row').forEach(tr => {
        const rowIndex = tr.dataset.rowIndex;
        const newTd = document.createElement('td');
        newTd.dataset.roomIndex = newRoomIndex;

        const needsRate2 = tr.classList.contains('js-we-row');
        const rateClass = needsRate2 ? 'js-rate2' : 'js-rate1';

        newTd.innerHTML = `<input type="number" class="input input-xs input-bordered w-full text-center ${rateClass}" data-row-index="${rowIndex}" data-room-index="${newRoomIndex}" value="" />`; // Add value=""

        // Insert before the meal cell if it exists in this row
        const mealCell = tr.querySelector('.js-meal-cell, .js-global-meal-cell');
        if (mealCell) {
            tr.insertBefore(newTd, mealCell);
        } else {
            tr.appendChild(newTd);
        }
    });

    // --- Update Counts and Spans ---
    const newRoomCount = updateRoomIndices(card); // Re-index rooms
    card.querySelector('.js-no-of-rooms').value = newRoomCount;
    updateCommentColspan(card, newRoomCount);
};

window.removeColumn = window.removeColumn || function (button) {
    if (!confirm("Are you sure you want to remove this room type column?")) {
        return;
    }
    const card = button.closest('.rate-card');
    if (!card) return;
    const thToRemove = button.closest('th.js-room-header'); // Target only room headers
    if (!thToRemove) return;

    // Calculate index relative to room headers ONLY
    let roomHeaderIndex = -1;
    const roomHeaders = card.querySelectorAll('.js-header-row-main .js-room-header');
    roomHeaders.forEach((th, index) => {
        if (th === thToRemove) {
            roomHeaderIndex = index; // 0-based index among room headers
        }
    });

    if (roomHeaderIndex === -1) {
        console.error("Could not determine room header index to remove.");
        return;
    }

    // --- Remove Header Cells ---
    thToRemove.remove();
    // Remove corresponding alignment TH in sub-header (find by index among all THs)
    // Index needs to account for non-room THs: Remove(0) + From(1) + To(2) + Type(3) + roomHeaderIndex
    const subHeaderCellToRemove = card.querySelectorAll(`.js-header-row-sub th`)[4 + roomHeaderIndex];
    if (subHeaderCellToRemove) {
        // Verify it has the correct data-room-index before removing (optional check)
        // console.log("Removing sub TH:", subHeaderCellToRemove, "at index:", 4 + roomHeaderIndex);
        subHeaderCellToRemove.remove();
    } else {
        console.warn("Sub header cell not found for room index:", roomHeaderIndex);
    }

    // --- Remove Body Cells ---
    const tbody = card.querySelector('.js-rate-table-body');
    if (!tbody) { console.error("Table body not found"); return; }
    tbody.querySelectorAll('.js-rate-row').forEach(tr => {
        // Find the TD corresponding to the roomHeaderIndex
        const tdToRemove = tr.querySelector(`td[data-room-index="${roomHeaderIndex}"]`);
        if (tdToRemove) {
            tdToRemove.remove();
        } else {
            // This might happen legitimately on rows that don't have room cells (e.g., header-like rows in tbody?)
            // console.warn("Could not find TD to remove in row:", tr, "at room index:", roomHeaderIndex);
        }
    });

    // --- Update Counts, Indices, and Spans ---
    const newRoomCount = updateRoomIndices(card); // Re-index rooms
    card.querySelector('.js-no-of-rooms').value = newRoomCount;
    updateCommentColspan(card, newRoomCount);
};

// --- Existing Functions (Modal Handling, Card Management, Saving) ---

// Modal handling
window.showModal = window.showModal || function (modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.showModal();
    } else {
        console.error("Modal not found:", modalId);
    }
};
window.closeModal = window.closeModal || function (modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.close();
    } else {
        console.error("Modal not found:", modalId);
    }
};

// Add Hotel Workflow
window.handleStep1 = window.handleStep1 || function (button) {
    const modal = button.closest('.modal-box'); // Find the modal context
    if (!modal) return;

    const data = {
        noOfDates: modal.querySelector('.js-modal-no-of-dates')?.value,
        noOfRoomTypes: modal.querySelector('.js-modal-no-of-room-types')?.value,
        cityId: modal.querySelector('.js-modal-city-select')?.value,
        mealPerRow: modal.querySelector('.js-modal-meal-per-row')?.checked
    };

    const dates = parseInt(data.noOfDates);
    const rooms = parseInt(data.noOfRoomTypes);
    if (!dates || dates <= 0) {
        alert('Please enter a valid number of dates (greater than 0)'); return;
    }
    if (!rooms || rooms <= 0) {
        alert('Please enter a valid number of room types (greater than 0)'); return;
    }
    if (!data.cityId) {
        alert('Please select a city'); return;
    }

    sessionStorage.setItem('step1Data', JSON.stringify(data));

    const step2Form = document.querySelector('#addHotelStep2 .js-step2-form');
    if (!step2Form) { console.error("Step 2 form container not found."); return; }

    step2Form.innerHTML = Array.from({ length: dates }, (_, i) => `
        <div class="form-control w-full">
            <label class="label">
                <span class="label-text">Rate Type for Date Range ${i + 1}</span>
            </label>
            <select class="select select-bordered w-full js-row-rate-select" data-row-index="${i}">
                <option value="1">Flat Rate</option>
                <option value="2">Weekend/Weekday Rate</option>
            </select>
        </div>
    `).join('');

    closeModal('addHotelStep1');
    showModal('addHotelStep2');
};

window.handleStep2 = window.handleStep2 || function (button) {
    const step1DataString = sessionStorage.getItem('step1Data');
    if (!step1DataString) { alert('Session data missing. Please start from step 1.'); return; }
    const step1Data = JSON.parse(step1DataString);

    const modal = button.closest('.modal-box');
    if (!modal) return;

    const rowRateSelects = modal.querySelectorAll('.js-row-rate-select');
    const rowRates = Array.from(rowRateSelects).map(select => ({
        row: parseInt(select.dataset.rowIndex) + 1, // Backend might expect 1-based
        selection: select.value
    }));

    const formData = new FormData();
    formData.append('noOfDates', step1Data.noOfDates);
    formData.append('noOfRoomTypes', step1Data.noOfRoomTypes);
    formData.append('cityId', step1Data.cityId);
    formData.append('cardId', window.rateCardState.cardId); // Use the next available ID
    formData.append('mealPerRow', step1Data.mealPerRow);
    formData.append('rowRates', JSON.stringify(rowRates));

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) { alert('Security token not found. Cannot proceed.'); console.error('Antiforgery token input not found.'); return; }

    fetch('/RateCard/AddCard', { // Ensure this endpoint exists and returns the correct partial HTML
        method: 'POST',
        headers: { 'RequestVerificationToken': tokenInput.value },
        body: formData
    })
        .then(response => {
            if (!response.ok) { throw new Error(`HTTP error! status: ${response.status}`); }
            return response.text();
        })
        .then(html => {
            const container = document.querySelector('.js-rate-card-container');
            if (container) {
                container.insertAdjacentHTML('beforeend', html);
                // Initialize controls for the new card if necessary (e.g., date pickers)
                // ...

                window.rateCardState.cardList.push(String(window.rateCardState.cardId));
                window.rateCardState.cardId++; // Increment for the next new card
                sessionStorage.removeItem('step1Data');
                updateOrder(); // Update order display after adding
            } else {
                console.error("Rate card container .js-rate-card-container not found.");
            }
        })
        .catch(error => {
            console.error('Error adding card:', error);
            alert('An error occurred while adding the hotel card. Please check console.');
        });

    closeModal('addHotelStep2');
};

// Card Management
window.removeCard = window.removeCard || function (button) {
    if (confirm('Are you sure you want to remove this card?')) {
        const cardElement = button.closest('.rate-card');
        if (cardElement) {
            const cardIdToRemove = cardElement.dataset.cardId;
            const index = window.rateCardState.cardList.indexOf(cardIdToRemove);
            if (index !== -1) {
                window.rateCardState.cardList.splice(index, 1);
            } else { console.warn("Card ID not found in state list:", cardIdToRemove); }
            cardElement.remove();
            updateOrder();
        } else { console.error("Could not find parent card element to remove."); }
    }
};

window.moveOrder = window.moveOrder || function (button, moveUp) {
    const mainCard = button.closest('.rate-card');
    if (!mainCard) return;
    const container = mainCard.parentNode;
    const cards = Array.from(container.querySelectorAll('.rate-card'));
    const currentIndex = cards.indexOf(mainCard);
    const newIndex = moveUp ? currentIndex - 1 : currentIndex + 1;

    if (newIndex >= 0 && newIndex < cards.length) {
        const otherCard = cards[newIndex];
        if (moveUp) { container.insertBefore(mainCard, otherCard); }
        else { container.insertBefore(mainCard, otherCard.nextSibling); }
        updateOrder();
    }
};

window.updateOrder = window.updateOrder || function () {
    const container = document.querySelector('.js-rate-card-container');
    if (!container) return;
    const cards = container.querySelectorAll('.rate-card');
    cards.forEach((card, index) => {
        const placementInput = card.querySelector('.js-placement-order');
        if (placementInput) { placementInput.value = index + 1; }
        // Update data-card-id as well if it's meant to represent placement order for JS state
        // card.dataset.cardId = index + 1; // Be careful if cardId has other meanings
    });

    // Update the global state list to reflect the new order
    window.rateCardState.cardList = Array.from(cards).map(card => card.dataset.cardId);
    // console.log("Updated cardList order:", window.rateCardState.cardList);
};

// Save Progress
window.saveCardProgress = window.saveCardProgress || function (close, showNotification = true) {
    const nameInput = document.querySelector('.js-rate-card-name');
    if (!nameInput?.value) {
        alert('Please add a name to reference this rate card');
        return;
    }
    const name = nameInput.value;

    const rateCardContainer = document.querySelector('.js-rate-card-container');
    if (!rateCardContainer) {
        console.error("Rate card container not found for saving.");
        return;
    }

    const cardElements = rateCardContainer.querySelectorAll('.rate-card');

    const data = Array.from(cardElements).map(card => {
        const getValue = (selector) => {
            const value = card.querySelector(selector)?.value;

            // If the value is a single comma (or just whitespace), treat it as empty.
            if (value && value.trim() === ',') {
                return '';
            }

            // Otherwise, return the value or an empty string if it's null/undefined.
            return value || '';
        };
        const getIntValue = (selector) => parseInt(card.querySelector(selector)?.value || '0', 10);
        const getBoolValue = (selector) => (card.querySelector(selector)?.value || 'false').toLowerCase() === 'true';

        const noOfRows = getIntValue('.js-no-of-rows');
        const noOfRooms = getIntValue('.js-no-of-rooms');
        const mealPerRow = getBoolValue('.js-meal-per-row-value');
        const selectedRooms = Array.from(card.querySelectorAll('.js-room-type-select')).map(select => select.value);

        const rowData = [];
        const rowElements = card.querySelectorAll('.js-rate-table-body .js-rate-row:not(.js-we-row)'); // Get primary rows
        rowElements.forEach(rowEl => {
            const rowIndex = rowEl.dataset.rowIndex;
            const rateTypeEnumInput = card.querySelector(`.js-rate-type-enum[data-row-index="${rowIndex}"]`);
            const mealNoteInput = card.querySelector(`.js-meal-note-row[data-row-index="${rowIndex}"]`); // Only used if mealPerRow

            const rates = [];
            // Find inputs based on the current structure for this specific row index
            const rate1Inputs = rowEl.querySelectorAll(`.js-rate1[data-row-index="${rowIndex}"]`);
            // Find the corresponding WE row (if it exists) and its rate2 inputs
            const weRow = card.querySelector(`.js-we-row[data-row-index="${rowIndex}"]`);
            const rate2Inputs = weRow ? weRow.querySelectorAll(`.js-rate2[data-row-index="${rowIndex}"]`) : [];

            // Iterate based on the number of rate1 inputs found, assuming rate2 inputs correspond
            rate1Inputs.forEach((r1Input, roomIdx) => {
                const r2Input = rate2Inputs[roomIdx]; // Find corresponding rate2 input by index
                rates.push({
                    rate1: r1Input.value || '0',
                    rate2: (rateTypeEnumInput?.value === '2' && r2Input) ? (r2Input.value || '0') : '0' // Include rate2 only if type is '2' and input exists
                });
            });

            // Ensure the number of rate entries matches the number of selected rooms for consistency
            if (rates.length < selectedRooms.length) {
                console.warn(`Mismatch between rate inputs found (${rates.length}) and selected rooms (${selectedRooms.length}) for row index ${rowIndex} in card ${card.dataset.cardId}. Padding rates.`);
                for (let k = rates.length; k < selectedRooms.length; k++) {
                    rates.push({ rate1: '0', rate2: '0' }); // Pad with default values
                }
            } else if (rates.length > selectedRooms.length) {
                console.warn(`More rate inputs found (${rates.length}) than selected rooms (${selectedRooms.length}) for row index ${rowIndex} in card ${card.dataset.cardId}. Truncating rates.`);
                rates.length = selectedRooms.length; // Truncate extra rates
            }


            rowData.push({
                from: card.querySelector(`.js-from-date[data-row-index="${rowIndex}"]`)?.value || '',
                to: card.querySelector(`.js-to-date[data-row-index="${rowIndex}"]`)?.value || '',
                selection: rateTypeEnumInput?.value || '1', // Default to '1' if enum input missing
                rates: rates,
                mealNote: mealPerRow ? (mealNoteInput?.value || '') : ''
            });
        });

        // Validate rowData length matches noOfRows?
        if (rowData.length !== noOfRows) {
            console.warn(`Data rows collected (${rowData.length}) does not match hidden count (${noOfRows}) for card ${card.dataset.cardId}. Using collected count.`);
            // Adjust noOfRows based on actual data found? Or trust the hidden input? Trusting hidden for now.
            // If rowData is wrong, there's an issue in the row collection loop above.
        }


        return {
            selectedHotel: getValue('.js-hotel-select'),
            selectedSupplier: getValue('.js-supplier-select'),
            numberOfRows: noOfRows, // Use count from updated hidden input
            numberOfRooms: noOfRooms, // Use count from updated hidden input
            selectedRooms: selectedRooms,
            rows: rowData,
            mealNotes: !mealPerRow ? getValue('.js-meal-notes') : '',
            additionalNotes: getValue('.js-additional-notes'),
            mealPerRow: mealPerRow,
            placementOrder: getIntValue('.js-placement-order')
        };
    }).sort((a, b) => a.placementOrder - b.placementOrder);

    // --- Prepare and Send Data ---
    const formData = new FormData();
    formData.append('model', JSON.stringify(data));
    formData.append('redirect', close);
    formData.append('name', name);
    const rateCardIdInput = document.querySelector('.js-rate-card-id');
    formData.append('rateCardId', rateCardIdInput?.value || '0');

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        alert('Security token not found. Cannot save.');
        console.error('Antiforgery token input not found.');
        return;
    }

    console.log("Saving data:", JSON.stringify(data)); // Log data being sent

    fetch('/RateCard/SaveProgress', {
        method: 'POST',
        headers: { 'RequestVerificationToken': tokenInput.value },
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                // Attempt to get more detailed error from response body
                return response.text().then(text => {
                    let errorDetail = text;
                    try { // Try parsing as JSON in case the error response is structured
                        const errorJson = JSON.parse(text);
                        errorDetail = errorJson.detail || errorJson.title || JSON.stringify(errorJson);
                    } catch (e) { /* Ignore parsing error, use raw text */ }
                    throw new Error(`Save failed! Status: ${response.status}. Details: ${errorDetail}`);
                });
            }
            return response.json();
        })
        .then(result => {
            console.log("Save response:", result);
            if (result.redirect === true || result.value === true) { // Check explicit redirect flag or legacy 'value'
                if (typeof htmx !== 'undefined') {
                    console.log("Redirecting via HTMX to /RateCard/List");
                    htmx.ajax('GET', '/RateCard/List', { target: '#main-content', swap: 'innerHTML' }); // Adjust target as needed
                } else {
                    console.log("Redirecting via window.location to /RateCard/List");
                    window.location.href = '/RateCard/List'; // Fallback redirect
                }
            } else {
                // Update the hidden rateCardId input if the save was successful and returned an ID (for new cards)
                if (result.id && rateCardIdInput && rateCardIdInput.value === '0') {
                    console.log("Updating Rate Card ID to:", result.id);
                    rateCardIdInput.value = result.id;
                }
                if (showNotification) {
                    alert('Progress Saved!'); // Replace with a less intrusive notification if possible
                }
            }
        })
        .catch((error) => {
            console.error('Error saving progress:', error);
            // Display a more informative error message
            alert(`An error occurred while saving: ${error.message}. Please check console or contact system admin.`);
        });
};

// Initialization and Auto-Save
window.initializeRateCard = window.initializeRateCard || function (initialState = null) {
    if (initialState) {
        // Ensure cardList contains strings if that's how you use them later
        initialState.cardList = (initialState.cardList || []).map(String);
        window.rateCardState = initialState;
        console.log("Rate card state initialized:", window.rateCardState);
    }

    // Initial update of placement order on load (if cards exist)
    if (window.rateCardState.cardList.length > 0) {
        updateOrder();
    }
    // Initial update of colspans/rowspans for existing cards on load (Edit page)
    document.querySelectorAll('.rate-card').forEach(card => {
        const roomCount = parseInt(card.querySelector('.js-no-of-rooms')?.value || '0', 10);
        const isMealPerRow = card.querySelector('.js-meal-per-row-value')?.value?.toLowerCase() === 'true';
        if (!isMealPerRow) {
            updateMealRowspan(card);
        }
        updateCommentColspan(card, roomCount);
    });


    // Auto-save functionality
    const autoSaveInterval = setInterval(() => {
        const nameInput = document.querySelector('.js-rate-card-name');
        const container = document.querySelector('.js-rate-card-container');
        // Only auto-save if there's a name and at least one card exists
        if (nameInput?.value && container?.querySelector('.rate-card')) {
            console.log("Auto-saving progress...");
            window.saveCardProgress(false, false); // Save without closing/notification
        }
    }, 60000); // 60 seconds

    // Clean up interval when navigating away
    window.addEventListener('beforeunload', () => {
        console.log("Clearing auto-save interval.");
        clearInterval(autoSaveInterval);
    });

    // Mark script as initialized (if loaded via HTMX partial updates)
    const scriptTag = document.querySelector('.js-rate-card-scripts-marker');
    if (scriptTag) scriptTag.dataset.initialized = 'true';
};