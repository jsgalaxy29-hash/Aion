let paletteHandler = null;
let paletteRef = null;

export function registerCommandPalette(dotNetRef) {
    paletteRef = dotNetRef;
    paletteHandler = (event) => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            if (paletteRef) {
                paletteRef.invokeMethodAsync('ShowFromShortcut');
            }
        }
    };

    document.addEventListener('keydown', paletteHandler);
}

export function disposeCommandPalette() {
    if (paletteHandler) {
        document.removeEventListener('keydown', paletteHandler);
        paletteHandler = null;
    }

    if (paletteRef) {
        paletteRef.dispose();
        paletteRef = null;
    }
}
