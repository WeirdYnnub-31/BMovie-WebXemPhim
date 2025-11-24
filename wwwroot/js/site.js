// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.theme = (function () {
    const KEY = 'theme';
    function apply(theme) {
        document.documentElement.classList.toggle('theme-dark', theme === 'dark');
    }
    function init() {
        const saved = localStorage.getItem(KEY);
        const theme = saved || (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        apply(theme);
    }
    function toggle() {
        const isDark = document.documentElement.classList.contains('theme-dark');
        const next = isDark ? 'light' : 'dark';
        localStorage.setItem(KEY, next);
        apply(next);
        return next;
    }
    return { init, toggle };
})();

// Copy to clipboard helper
window.copyToClipboard = async function(text) {
    try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            await navigator.clipboard.writeText(text);
            return true;
        } else {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            document.body.appendChild(textArea);
            textArea.select();
            const success = document.execCommand('copy');
            document.body.removeChild(textArea);
            return success;
        }
    } catch (err) {
        console.error('Failed to copy text:', err);
        return false;
    }
};