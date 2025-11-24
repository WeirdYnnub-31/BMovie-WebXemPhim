// Enhanced Theme Toggle with GSAP Animations
(function() {
    'use strict';

    // Don't return early - we have fallback support

    const THEME_KEY = 'bm-theme';
    const TRANSITION_DURATION = 0.6;

    // Theme configuration
    const themes = {
        dark: {
            name: 'dark',
            icon: '🌙',
            iconAlt: '🌜',
            label: 'Chế độ tối'
        },
        light: {
            name: 'light',
            icon: '☀️',
            iconAlt: '☀️',
            label: 'Chế độ sáng'
        }
    };

    // Get current theme
    function getCurrentTheme() {
        const body = document.body;
        const html = document.documentElement;
        
        if (body.classList.contains('theme-dark') || html.classList.contains('theme-dark')) {
            return 'dark';
        }
        if (body.classList.contains('theme-light') || html.classList.contains('theme-light')) {
            return 'light';
        }
        
        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }

    // Apply theme with smooth transition
    function applyTheme(theme, animate = true) {
        const body = document.body;
        const html = document.documentElement;
        const isDark = theme === 'dark';
        const canAnimate = animate && typeof gsap !== 'undefined';

        if (canAnimate) {
            // Create overlay for smooth transition
            const overlay = document.createElement('div');
            overlay.id = 'theme-transition-overlay';
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: ${isDark ? '#000' : '#fff'};
                z-index: 9999;
                opacity: 0;
                pointer-events: none;
            `;
            document.body.appendChild(overlay);

            // Animate overlay in
            gsap.to(overlay, {
                opacity: 1,
                duration: TRANSITION_DURATION / 2,
                ease: 'power2.inOut',
                onComplete: () => {
                    // Change theme classes
                    body.classList.remove('theme-dark', 'theme-light');
                    html.classList.remove('theme-dark', 'theme-light');
                    
                    if (isDark) {
                        body.classList.add('theme-dark');
                        html.classList.add('theme-dark');
                    } else {
                        body.classList.add('theme-light');
                        html.classList.add('theme-light');
                    }

                    // Update theme toggle button
                    updateThemeButton(theme);

                    // Animate overlay out
                    gsap.to(overlay, {
                        opacity: 0,
                        duration: TRANSITION_DURATION / 2,
                        ease: 'power2.inOut',
                        onComplete: () => {
                            overlay.remove();
                            // Trigger custom event
                            document.dispatchEvent(new CustomEvent('themeChanged', { 
                                detail: { theme: theme } 
                            }));
                        }
                    });
                }
            });
        } else {
            // Apply without animation
            body.classList.remove('theme-dark', 'theme-light');
            html.classList.remove('theme-dark', 'theme-light');
            
            if (isDark) {
                body.classList.add('theme-dark');
                html.classList.add('theme-dark');
            } else {
                body.classList.add('theme-light');
                html.classList.add('theme-light');
            }
            
            updateThemeButton(theme);

            if (!canAnimate) {
                document.dispatchEvent(new CustomEvent('themeChanged', { 
                    detail: { theme: theme } 
                }));
            }
        }
    }

    // Update theme toggle button
    function updateThemeButton(theme) {
        const toggleBtn = document.getElementById('bm-theme-toggle');
        if (!toggleBtn) {
            console.warn('Theme toggle button not found');
            return;
        }

        const nextTheme = theme === 'dark' ? 'light' : 'dark';
        const nextConfig = themes[nextTheme];

        // Find icon element (could be direct child or in span)
        const iconElement = toggleBtn.querySelector('.theme-icon') || toggleBtn;
        
        // Function to update icon
        const updateIcon = () => {
            if (iconElement === toggleBtn) {
                toggleBtn.innerHTML = nextConfig.icon;
            } else {
                iconElement.textContent = nextConfig.icon;
            }
            toggleBtn.setAttribute('title', `Chuyển sang ${nextConfig.label}`);
            toggleBtn.setAttribute('aria-label', `Chuyển sang ${nextConfig.label}`);
        };
        
        // Check if GSAP is available for animations
        if (typeof gsap !== 'undefined') {
            // Animate icon change
            gsap.to(toggleBtn, {
                rotation: 360,
                scale: 1.2,
                duration: 0.3,
                ease: 'back.out(1.7)',
                onComplete: () => {
                    updateIcon();
                    
                    gsap.to(toggleBtn, {
                        rotation: 0,
                        scale: 1,
                        duration: 0.3,
                        ease: 'back.out(1.7)'
                    });
                }
            });

            // Add pulse effect
            gsap.to(toggleBtn, {
                boxShadow: '0 0 20px rgba(255, 255, 255, 0.5)',
                duration: 0.3,
                yoyo: true,
                repeat: 1,
                ease: 'power2.inOut'
            });
        } else {
            // Update icon immediately without animation
            updateIcon();
        }
    }

    // Enhanced toggle function - override any existing function
    window.bmToggleTheme = function() {
        console.log('bmToggleTheme called');
        
        const currentTheme = getCurrentTheme();
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        console.log('Current theme:', currentTheme, 'Next theme:', nextTheme);
        
        // Save to localStorage first
        try {
            localStorage.setItem(THEME_KEY, nextTheme);
        } catch (e) {
            console.warn('Failed to save theme preference:', e);
        }

        // Check if GSAP is available for animations
        if (typeof gsap !== 'undefined') {
            // Apply theme with animation
            applyTheme(nextTheme, true);
        } else {
            // Fallback to simple toggle without animation
            const body = document.body;
            const html = document.documentElement;
            
            body.classList.remove('theme-dark', 'theme-light');
            html.classList.remove('theme-dark', 'theme-light');
            
            if (nextTheme === 'dark') {
                body.classList.add('theme-dark');
                html.classList.add('theme-dark');
            } else {
                body.classList.add('theme-light');
                html.classList.add('theme-light');
            }
            
            // Update button icon immediately
            const toggleBtn = document.getElementById('bm-theme-toggle');
            if (toggleBtn) {
                const iconElement = toggleBtn.querySelector('.theme-icon') || toggleBtn;
                const icon = nextTheme === 'dark' ? '☀️' : '🌙';
                if (iconElement === toggleBtn) {
                    toggleBtn.innerHTML = icon;
                } else {
                    iconElement.textContent = icon;
                }
                toggleBtn.setAttribute('title', `Chuyển sang ${nextTheme === 'dark' ? 'Chế độ tối' : 'Chế độ sáng'}`);
            }
        }
    };

    // Initialize theme on page load
    function initTheme() {
        let savedTheme = null;
        
        try {
            savedTheme = localStorage.getItem(THEME_KEY);
        } catch (e) {
            console.warn('Failed to read theme preference:', e);
        }

        const theme = savedTheme || getCurrentTheme();
        applyTheme(theme, false); // No animation on initial load

        // Listen for system theme changes
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                if (!localStorage.getItem(THEME_KEY)) {
                    const systemTheme = e.matches ? 'dark' : 'light';
                    applyTheme(systemTheme, true);
                }
            });
        }
    }

    // Enhanced theme toggle button styling
    function enhanceThemeButton() {
        const toggleBtn = document.getElementById('bm-theme-toggle');
        if (!toggleBtn) return;

        // Add enhanced styling
        toggleBtn.style.cssText += `
            position: relative;
            transition: all 0.3s ease;
            overflow: hidden;
        `;

        // Add hover effect
        toggleBtn.addEventListener('mouseenter', function() {
            gsap.to(toggleBtn, {
                scale: 1.1,
                rotation: 15,
                duration: 0.3,
                ease: 'back.out(1.7)'
            });
        });

        toggleBtn.addEventListener('mouseleave', function() {
            gsap.to(toggleBtn, {
                scale: 1,
                rotation: 0,
                duration: 0.3,
                ease: 'back.out(1.7)'
            });
        });

        // Add click ripple effect
        toggleBtn.addEventListener('click', function(e) {
            const ripple = document.createElement('span');
            const rect = toggleBtn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                border-radius: 50%;
                background: rgba(255, 255, 255, 0.3);
                left: ${x}px;
                top: ${y}px;
                pointer-events: none;
                transform: scale(0);
            `;

            toggleBtn.appendChild(ripple);

            gsap.to(ripple, {
                scale: 2,
                opacity: 0,
                duration: 0.6,
                ease: 'power2.out',
                onComplete: () => ripple.remove()
            });
        });
    }

    // Animate elements on theme change
    function animateElementsOnThemeChange(theme) {
        const isDark = theme === 'dark';
        
        // Animate cards
        const cards = document.querySelectorAll('.glass-card, .card, .movie-card');
        cards.forEach((card, index) => {
            gsap.from(card, {
                opacity: 0.7,
                scale: 0.98,
                duration: 0.4,
                delay: index * 0.02,
                ease: 'power2.out'
            });
        });

        // Animate text elements
        const textElements = document.querySelectorAll('h1, h2, h3, .section-title, p');
        textElements.forEach((el, index) => {
            gsap.from(el, {
                opacity: 0.8,
                y: -5,
                duration: 0.3,
                delay: index * 0.01,
                ease: 'power2.out'
            });
        });

        // Animate buttons
        const buttons = document.querySelectorAll('.btn, button');
        buttons.forEach((btn, index) => {
            gsap.from(btn, {
                opacity: 0.9,
                scale: 0.95,
                duration: 0.3,
                delay: index * 0.01,
                ease: 'back.out(1.2)'
            });
        });
    }

    // Listen for theme changes
    document.addEventListener('themeChanged', function(e) {
        const theme = e.detail.theme;
        animateElementsOnThemeChange(theme);
    });

    // Initialize when DOM is ready
    function initializeThemeToggle() {
        initTheme();
        enhanceThemeButton();
        
        // Ensure button has correct initial icon
        const toggleBtn = document.getElementById('bm-theme-toggle');
        if (toggleBtn) {
            const currentTheme = getCurrentTheme();
            const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
            const nextConfig = themes[nextTheme];
            const iconElement = toggleBtn.querySelector('.theme-icon') || toggleBtn;
            
            // Set initial icon based on current theme (show what it will switch TO)
            if (iconElement === toggleBtn) {
                if (!toggleBtn.textContent.trim() || toggleBtn.textContent.includes('🌙') || toggleBtn.textContent.includes('☀️')) {
                    toggleBtn.innerHTML = nextConfig.icon;
                }
            } else {
                iconElement.textContent = nextConfig.icon;
            }
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeThemeToggle);
    } else {
        // Use setTimeout to ensure GSAP is loaded
        setTimeout(initializeThemeToggle, 100);
    }

    // Export for external use
    window.bmTheme = {
        toggle: window.bmToggleTheme,
        set: function(theme) {
            if (themes[theme]) {
                try {
                    localStorage.setItem(THEME_KEY, theme);
                } catch (e) {
                    console.warn('Failed to save theme preference:', e);
                }
                applyTheme(theme, true);
            }
        },
        get: getCurrentTheme
    };

    console.log('Enhanced theme toggle initialized');
})();

