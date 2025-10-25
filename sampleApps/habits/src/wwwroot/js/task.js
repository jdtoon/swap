window.initiateTaskList =
  window.initiateTaskList ||
  function (url) {
    var el = document.getElementById("task-lists-sortable");
    var sortable = Sortable.create(el, {
      animation: 150,
      delay: 100,
      onEnd: function (evt) {
        const itemElement = evt.item;
        const itemId = parseInt(itemElement.getAttribute("data-id"));
        const newIndex = evt.newIndex;

        htmx
          .ajax("POST", url, {
            values: {
              id: itemId.toString(),
              newPosition: (newIndex + 1).toString(),
            },
            target: "#task-lists-sortable",
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

window.closeTaskModal =
  window.closeTaskModal ||
  function (id) {
    document.getElementById(`edit-modal-${id}`).close();
    document.getElementById(`edit-modal-${id}`).remove();
  };

window.handleAfterSettle =
  window.handleAfterSettle ||
  function (evt) {
    updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
  };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);
