async function waitForBlinkID() {
    return new Promise(resolve => {
        const check = () => {
            if (window.BlinkID) resolve();
            else setTimeout(check, 50);
        };
        check();
    });
}

(async () => {
    await waitForBlinkID();           // this will NOW work again
    console.log("BlinkID SDK Available!");

    const sdk = await BlinkID.load({
        licenseKey: window.BlinkIDLicenseKey,
        engineLocation: "~/Content/BlinkId/resources/full/advanced/"
    });

    const recognizer = await sdk.createBlinkIdRecognizer();

    window.addEventListener("message", async (event) => {
        if (event.data.type === "blinkid-upload-file") {
            console.log('blinkid-upload-file event');
            const base64 = event.data.fileData.split(",")[1];

            try {
                const result = await recognizer.processImage(base64);

                window.parent.postMessage({
                    type: "blinkid-upload-result",
                    data: result
                }, "*");
            } catch (err) {
                window.parent.postMessage({
                    type: "blinkid-upload-error",
                    data: err.message
                }, "*");
            }
        }
    });
})();
