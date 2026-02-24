window.chatHelpers = {
    scrollToBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    },
    isAtBottom: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            // Use a 20px threshold to account for rounding and minor zoom levels
            return element.scrollHeight - element.scrollTop - element.clientHeight < 20;
        }
        return false;
    }
};
