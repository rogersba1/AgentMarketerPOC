// Chat functionality for Agent Marketer
window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Initialize any additional chat features when the page loads
window.initializeChat = () => {
    // Add any future chat initialization logic here
    console.log('Chat initialized');
};

// Auto-scroll to bottom when new messages are added
window.autoScrollChat = (element) => {
    if (element) {
        const isNearBottom = element.scrollTop + element.clientHeight >= element.scrollHeight - 100;
        if (isNearBottom) {
            setTimeout(() => {
                element.scrollTop = element.scrollHeight;
            }, 100);
        }
    }
};

// Focus input when page loads
window.focusChatInput = () => {
    const input = document.querySelector('.chat-input input[type="text"]');
    if (input) {
        input.focus();
    }
};
