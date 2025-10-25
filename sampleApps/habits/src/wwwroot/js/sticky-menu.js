window.handleNavIconUpdate =
  window.handleNavIconUpdate ||
  function () {
    const currentPath = window.location.pathname.toLowerCase();
    const navIcons = document.querySelectorAll(".bottom-0 img");

    navIcons.forEach((icon) => {
      const path = icon.dataset.path;
      if (
        path === "/" &&
        (currentPath === "/" ||
          currentPath === "/home" ||
          currentPath === "/home/index" ||
          currentPath === "")
      ) {
        icon.src = icon.dataset.activeSrc;
      } else if (currentPath.includes(path) && path !== "/") {
        icon.src = icon.dataset.activeSrc;
      } else {
        icon.src = icon.dataset.inactiveSrc;
      }
    });
  };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleNavIconUpdate);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleNavIconUpdate);
