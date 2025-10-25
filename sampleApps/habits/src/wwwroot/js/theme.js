window.updateMainNavbarBackgroundColor =
    window.updateMainNavbarBackgroundColor ||
    function (elementId, newBackgroundColor, newFontColor = null) {
        const element = document.getElementById(elementId);
        if (element) {
            const currentBackgroundColor = getComputedStyle(element).backgroundColor;

            // Convert newBackgroundColor to RGB format if it's a hex color
            const newBackgroundColorRgb = hexToRgb(newBackgroundColor);

            // Update only if the current color is different
            if (currentBackgroundColor !== newBackgroundColorRgb) {
                element.style.backgroundColor = newBackgroundColor;

                // Optionally update the font color
                if (newFontColor) {
                    element.style.color = newFontColor;
                }
            }
        }
    };

// Helper function to convert hex to RGB
window.hexToRgb =
    window.hexToRgb ||
    function (hex) {
        const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
        hex = hex.replace(shorthandRegex, (m, r, g, b) => r + r + g + g + b + b);

        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result
            ? `rgb(${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(
                result[3],
                16
            )})`
            : null;
    };