window.initQRCode =
  window.initQRCode ||
  function () {
    const qrCodeElement = document.getElementById("qrCode");
    const dataElement = document.getElementById("qrCodeData");

    if (qrCodeElement && dataElement) {
      const uri = dataElement.getAttribute("data-url");
      if (uri) {
        new QRCode(qrCodeElement, {
          text: uri,
          width: 150,
          height: 150,
        });
      }
    }
  };

window.addEventListener("load", window.initQRCode);
