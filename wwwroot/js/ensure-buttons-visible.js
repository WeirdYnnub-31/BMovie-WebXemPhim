// CRITICAL: Ensure all buttons are always visible on all pages
(function() {
    'use strict';
    
    function ensureButtonsVisible() {
        // Select all buttons, links that look like buttons, and form buttons
        const selectors = [
            'button',
            '.btn',
            'input[type="submit"]',
            'input[type="button"]',
            'a.btn',
            '.dropdown-toggle',
            '.navbar-toggler',
            'form button[type="submit"]',
            '.hero-buttons .btn',
            '.hero-btn-primary',
            '.hero-btn-secondary'
        ];
        
        selectors.forEach(selector => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(el => {
                // Skip if element is intentionally hidden (has d-none class)
                if (el.classList.contains('d-none') || el.hasAttribute('hidden')) {
                    return;
                }
                
                // Force visibility
                el.style.setProperty('opacity', '1', 'important');
                el.style.setProperty('visibility', 'visible', 'important');
                el.style.setProperty('display', '', 'important');
                
                // Remove any inline styles that might hide it
                if (el.style.opacity === '0') {
                    el.style.removeProperty('opacity');
                }
                if (el.style.visibility === 'hidden') {
                    el.style.removeProperty('visibility');
                }
                if (el.style.display === 'none') {
                    el.style.removeProperty('display');
                }
            });
        });
        
        // Also ensure dropdown menus are accessible
        const dropdowns = document.querySelectorAll('.dropdown, .dropdown-menu');
        dropdowns.forEach(dropdown => {
            dropdown.style.setProperty('opacity', '1', 'important');
            dropdown.style.setProperty('visibility', 'visible', 'important');
        });
    }
    
    // Run immediately
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', ensureButtonsVisible);
    } else {
        ensureButtonsVisible();
    }
    
    // Run multiple times to catch dynamically added buttons
    setTimeout(ensureButtonsVisible, 10);
    setTimeout(ensureButtonsVisible, 50);
    setTimeout(ensureButtonsVisible, 100);
    setTimeout(ensureButtonsVisible, 200);
    setTimeout(ensureButtonsVisible, 500);
    setTimeout(ensureButtonsVisible, 1000);
    setTimeout(ensureButtonsVisible, 2000);
    
    // Watch for new buttons being added
    if (typeof MutationObserver !== 'undefined') {
        const observer = new MutationObserver(function(mutations) {
            let shouldCheck = false;
            mutations.forEach(function(mutation) {
                if (mutation.addedNodes.length > 0) {
                    shouldCheck = true;
                }
                if (mutation.type === 'attributes' && mutation.attributeName === 'style') {
                    const target = mutation.target;
                    if (target.tagName === 'BUTTON' || target.classList.contains('btn') || target.classList.contains('dropdown-toggle')) {
                        if (target.style.opacity === '0' || target.style.visibility === 'hidden' || target.style.display === 'none') {
                            target.style.setProperty('opacity', '1', 'important');
                            target.style.setProperty('visibility', 'visible', 'important');
                            target.style.setProperty('display', '', 'important');
                        }
                    }
                }
            });
            if (shouldCheck) {
                ensureButtonsVisible();
            }
        });
        
        if (document.body) {
            observer.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['style', 'class']
            });
        } else {
            document.addEventListener('DOMContentLoaded', function() {
                observer.observe(document.body, {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['style', 'class']
                });
            });
        }
    }
    
    // Override GSAP if it exists
    if (typeof gsap !== 'undefined') {
        const originalSet = gsap.set;
        gsap.set = function(targets, vars) {
            const result = originalSet.call(this, targets, vars);
            // If setting opacity to 0 or visibility to hidden, don't apply to buttons
            if (vars && (vars.opacity === 0 || vars.visibility === 'hidden')) {
                const elements = Array.isArray(targets) ? targets : [targets];
                elements.forEach(el => {
                    if (el && (el.tagName === 'BUTTON' || el.classList?.contains('btn') || el.classList?.contains('dropdown-toggle'))) {
                        if (!el.classList.contains('d-none') && !el.hasAttribute('hidden')) {
                            el.style.setProperty('opacity', '1', 'important');
                            el.style.setProperty('visibility', 'visible', 'important');
                        }
                    }
                });
            }
            return result;
        };
    }
})();

