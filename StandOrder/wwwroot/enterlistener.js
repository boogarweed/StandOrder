window.listenForGlobalEnter = function (dotNetHelper) {
    if (window._globalEnterListener) return; // Avoid multiple listeners
    window._globalEnterListener = function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('AddProductFromJs');
        }
    };
    document.addEventListener('keydown', window._globalEnterListener);
};
window.removeGlobalEnterListener = function () {
    if (window._globalEnterListener) {
        document.removeEventListener('keydown', window._globalEnterListener);
        window._globalEnterListener = null;
    }
};
