window.handleAfterSettle =
    window.handleAfterSettle ||
    function (evt) {
        // Call the color update after HTMX has settled
        updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
    };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);

window.startBarcodeScanner =
    window.startBarcodeScanner ||
    function () {
    // Using QuaggaJS for barcode scanning
    Quagga.init({
        inputStream: {
            name: "Live",
            type: "LiveStream",
            target: document.querySelector("#interactive"),
            constraints: {
                facingMode: "environment"
            },
        },
        decoder: {
            readers: ["ean_reader", "ean_8_reader", "code_128_reader"]
        }
    }, function (err) {
        if (err) {
            console.error(err);
            return;
        }
        Quagga.start();
    });

    Quagga.onDetected(function (result) {
        var code = result.codeResult.code;
        document.getElementById('barcode-input').value = code;
        Quagga.stop();
    });
} 