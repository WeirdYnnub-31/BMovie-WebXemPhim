// GSAP Animations for BMovie
(function() {
    'use strict';

    // Wait for DOM and GSAP to be ready
    if (typeof gsap === 'undefined') {
        console.warn('GSAP is not loaded');
        return;
    }

    // Plugins should be registered in gsap-plugins-setup.js
    // Check if ScrollTrigger is available
    if (typeof ScrollTrigger === 'undefined') {
        console.warn('ScrollTrigger plugin is not loaded');
    }

    // Initialize animations when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initPageAnimations();
        initNavbarAnimations();
        initCardAnimations();
        initButtonAnimations();
        initTableAnimations();
        initFormAnimations();
        initScrollAnimations();
        initAdvancedAnimations();
    });

    // Page load animations
    function initPageAnimations() {
        // Animate page content on load
        gsap.from('body', {
            opacity: 0,
            duration: 0.3,
            ease: 'power2.out'
        });

        // Animate main content
        gsap.from('main', {
            opacity: 0,
            y: 20,
            duration: 0.6,
            delay: 0.2,
            ease: 'power2.out'
        });

        // Animate header - ensure visible first and NEVER hide it
        const header = document.querySelector('header');
        if (header) {
            // CRITICAL: Set inline styles to ensure visibility - MUST be first
            header.style.setProperty('opacity', '1', 'important');
            header.style.setProperty('transform', 'translateY(0)', 'important');
            header.style.setProperty('visibility', 'visible', 'important');
            header.style.setProperty('display', 'block', 'important');
            
            // Also set for navbar inside header
            const navbar = header.querySelector('nav.navbar');
            if (navbar) {
                navbar.style.setProperty('opacity', '1', 'important');
                navbar.style.setProperty('transform', 'translateY(0)', 'important');
                navbar.style.setProperty('visibility', 'visible', 'important');
                navbar.style.setProperty('display', 'flex', 'important');
            }
            
            // Only animate if GSAP is available AND header is already visible
            // Use 'fromTo' to ensure it starts visible
            if (typeof gsap !== 'undefined') {
                gsap.fromTo(header, 
                    {
                        opacity: 1,
                        y: 0
                    },
                    {
                        opacity: 1,
                        y: 0,
                        duration: 0.3,
                        ease: 'power2.out',
                        onStart: () => {
                            // Ensure it stays visible during animation
                            header.style.setProperty('opacity', '1', 'important');
                            header.style.setProperty('transform', 'translateY(0)', 'important');
                        },
                        onComplete: () => {
                            // Ensure it stays visible after animation
                            header.style.setProperty('opacity', '1', 'important');
                            header.style.setProperty('transform', 'translateY(0)', 'important');
                        }
                    }
                );
            }
        }

        // Animate footer
        gsap.from('footer', {
            opacity: 0,
            y: 20,
            duration: 0.5,
            delay: 0.3,
            ease: 'power2.out'
        });
    }

    // Navbar animations
    function initNavbarAnimations() {
        const navbar = document.querySelector('nav.navbar');
        if (!navbar) return;

        // CRITICAL: Ensure navbar is visible first
        navbar.style.opacity = '1';
        navbar.style.transform = 'translateY(0)';
        navbar.style.visibility = 'visible';
        navbar.style.display = '';

        // Animate navbar brand - ensure visible first
        const navbarBrand = document.querySelector('.navbar-brand');
        if (navbarBrand) {
            navbarBrand.style.opacity = '1';
            navbarBrand.style.transform = 'translateX(0)';
            navbarBrand.style.visibility = 'visible';
            
            gsap.from(navbarBrand, {
                opacity: 0,
                x: -30,
                duration: 0.6,
                delay: 0.1,
                ease: 'back.out(1.7)'
            });
        }

        // Animate navbar items - ensure visible first
        const navItems = navbar.querySelectorAll('.nav-link, .dropdown-toggle, .btn');
        navItems.forEach(item => {
            item.style.opacity = '1';
            item.style.transform = 'translateY(0)';
            item.style.visibility = 'visible';
        });
        
        gsap.from(navItems, {
            opacity: 0,
            y: -10,
            duration: 0.4,
            stagger: 0.05,
            delay: 0.2,
            ease: 'power2.out'
        });

        // Navbar scroll effect - but ensure it never hides completely
        let lastScroll = 0;
        window.addEventListener('scroll', function() {
            const currentScroll = window.pageYOffset;
            if (currentScroll > 100) {
                if (currentScroll > lastScroll) {
                    // Scrolling down - hide navbar but ensure it's still accessible
                    gsap.to(navbar, {
                        y: -100,
                        duration: 0.3,
                        ease: 'power2.inOut',
                        onComplete: () => {
                            // Ensure navbar is still visible (just moved up)
                            navbar.style.visibility = 'visible';
                            navbar.style.opacity = '1';
                        }
                    });
                } else {
                    // Scrolling up - show navbar
                    gsap.to(navbar, {
                        y: 0,
                        duration: 0.3,
                        ease: 'power2.inOut',
                        onComplete: () => {
                            navbar.style.visibility = 'visible';
                            navbar.style.opacity = '1';
                        }
                    });
                }
            } else {
                // Near top - always show navbar
                gsap.to(navbar, {
                    y: 0,
                    duration: 0.3,
                    ease: 'power2.inOut',
                    onComplete: () => {
                        navbar.style.visibility = 'visible';
                        navbar.style.opacity = '1';
                    }
                });
            }
            lastScroll = currentScroll;
        });
    }

    // Card animations
    function initCardAnimations() {
        // Animate cards on load
        const cards = document.querySelectorAll('.card, .glass-card, .movie-card, [class*="movie-card"], [class*="card"]');
        
        if (cards.length > 0) {
            // CRITICAL: Ensure all cards are visible first
            cards.forEach(card => {
                card.style.setProperty('opacity', '1', 'important');
                card.style.setProperty('transform', 'translateY(0)', 'important');
                card.style.setProperty('visibility', 'visible', 'important');
            });
            
            // Only animate if ScrollTrigger is available
            if (typeof ScrollTrigger !== 'undefined') {
                gsap.fromTo(cards, 
                    {
                        opacity: 1,
                        y: 0
                    },
                    {
                        opacity: 1,
                        y: 0,
                        duration: 0.6,
                        stagger: 0.1,
                        scrollTrigger: {
                            trigger: cards[0],
                            start: 'top 80%',
                            toggleActions: 'play none none none'
                        },
                        ease: 'power2.out',
                        onStart: () => {
                            // Ensure cards stay visible during animation
                            cards.forEach(card => {
                                card.style.setProperty('opacity', '1', 'important');
                                card.style.setProperty('transform', 'translateY(0)', 'important');
                            });
                        },
                        onComplete: () => {
                            // Ensure cards stay visible after animation
                            cards.forEach(card => {
                                card.style.setProperty('opacity', '1', 'important');
                                card.style.setProperty('transform', 'translateY(0)', 'important');
                            });
                        }
                    }
                );
            }
        }

        // Card hover effects with enhanced animations
        cards.forEach(card => {
            // Enhanced hover effect
            card.addEventListener('mouseenter', function() {
                gsap.to(card, {
                    y: -8,
                    scale: 1.03,
                    boxShadow: '0 10px 30px rgba(0, 0, 0, 0.5)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
                
                // Animate overlay if exists
                const overlay = card.querySelector('.overlay, .movie-card-overlay');
                if (overlay) {
                    gsap.to(overlay, {
                        opacity: 1,
                        duration: 0.3,
                        ease: 'power2.out'
                    });
                }
            });

            card.addEventListener('mouseleave', function() {
                gsap.to(card, {
                    y: 0,
                    scale: 1,
                    boxShadow: '0 0 0 rgba(0, 0, 0, 0)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
                
                const overlay = card.querySelector('.overlay, .movie-card-overlay');
                if (overlay) {
                    gsap.to(overlay, {
                        opacity: 0,
                        duration: 0.3,
                        ease: 'power2.out'
                    });
                }
            });
        });
    }

    // Button animations
    function initButtonAnimations() {
        const buttons = document.querySelectorAll('button, .btn, a.btn, input[type="submit"]');
        
        buttons.forEach(btn => {
            // Hover effect
            btn.addEventListener('mouseenter', function() {
                gsap.to(btn, {
                    scale: 1.05,
                    duration: 0.2,
                    ease: 'power2.out'
                });
            });

            btn.addEventListener('mouseleave', function() {
                gsap.to(btn, {
                    scale: 1,
                    duration: 0.2,
                    ease: 'power2.out'
                });
            });

            // Click effect
            btn.addEventListener('mousedown', function() {
                gsap.to(btn, {
                    scale: 0.95,
                    duration: 0.1,
                    ease: 'power2.out'
                });
            });

            btn.addEventListener('mouseup', function() {
                gsap.to(btn, {
                    scale: 1.05,
                    duration: 0.1,
                    ease: 'power2.out'
                });
            });
        });

        // Animate buttons on load
        gsap.from(buttons, {
            opacity: 0,
            scale: 0.8,
            duration: 0.4,
            stagger: 0.03,
            delay: 0.3,
            ease: 'back.out(1.7)'
        });
    }

    // Table animations
    function initTableAnimations() {
        const tables = document.querySelectorAll('table');
        
        tables.forEach(table => {
            const rows = table.querySelectorAll('tbody tr');
            
            if (rows.length > 0) {
                // Animate rows on load
                gsap.from(rows, {
                    opacity: 0,
                    x: -30,
                    duration: 0.5,
                    stagger: 0.08,
                    scrollTrigger: {
                        trigger: table,
                        start: 'top 80%',
                        toggleActions: 'play none none none'
                    },
                    ease: 'power2.out'
                });
            }

            // Enhanced row hover effects
            rows.forEach((row, index) => {
                row.addEventListener('mouseenter', function() {
                    gsap.to(row, {
                        x: 8,
                        backgroundColor: 'rgba(255, 255, 255, 0.08)',
                        scale: 1.01,
                        duration: 0.25,
                        ease: 'power2.out'
                    });
                    
                    // Animate buttons in row
                    const buttons = row.querySelectorAll('button, .btn, a');
                    gsap.from(buttons, {
                        scale: 0.8,
                        opacity: 0.7,
                        duration: 0.2,
                        stagger: 0.05,
                        ease: 'back.out(1.7)'
                    });
                });

                row.addEventListener('mouseleave', function() {
                    gsap.to(row, {
                        x: 0,
                        backgroundColor: 'transparent',
                        scale: 1,
                        duration: 0.25,
                        ease: 'power2.out'
                    });
                });
            });
        });
    }

    // Form animations
    function initFormAnimations() {
        const inputs = document.querySelectorAll('input, textarea, select');
        
        inputs.forEach(input => {
            // Focus animation
            input.addEventListener('focus', function() {
                gsap.to(input, {
                    scale: 1.02,
                    boxShadow: '0 0 10px rgba(59, 130, 246, 0.5)',
                    duration: 0.2,
                    ease: 'power2.out'
                });
            });

            input.addEventListener('blur', function() {
                gsap.to(input, {
                    scale: 1,
                    boxShadow: '0 0 0 rgba(59, 130, 246, 0)',
                    duration: 0.2,
                    ease: 'power2.out'
                });
            });
        });

        // Animate forms on load
        const forms = document.querySelectorAll('form');
        gsap.from(forms, {
            opacity: 0,
            y: 20,
            duration: 0.5,
            stagger: 0.1,
            ease: 'power2.out'
        });
    }

    // Scroll animations
    function initScrollAnimations() {
        if (typeof ScrollTrigger === 'undefined') return;

        // Animate elements on scroll
        const animateElements = document.querySelectorAll('.section-title, h1, h2, h3, .neon-btn, .glass-card');
        
        animateElements.forEach(el => {
            gsap.from(el, {
                opacity: 0,
                y: 50,
                duration: 0.8,
                scrollTrigger: {
                    trigger: el,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power3.out'
            });
        });

        // Parallax effect for hero sections
        const heroSections = document.querySelectorAll('.hero, [class*="hero"]');
        heroSections.forEach(hero => {
            gsap.to(hero, {
                y: -50,
                scrollTrigger: {
                    trigger: hero,
                    start: 'top top',
                    end: 'bottom top',
                    scrub: true
                }
            });
        });
    }

    // Smooth scroll for anchor links using ScrollToPlugin
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const href = this.getAttribute('href');
            if (href === '#' || href === '') return;
            
            const target = document.querySelector(href);
            if (target && typeof ScrollToPlugin !== 'undefined') {
                e.preventDefault();
                gsap.to(window, {
                    duration: 1,
                    scrollTo: {
                        y: target,
                        offsetY: 80
                    },
                    ease: 'power2.inOut'
                });
            }
        });
    });

    // Loading animation for async content
    window.bmAnimateContent = function(selector) {
        const elements = document.querySelectorAll(selector);
        gsap.from(elements, {
            opacity: 0,
            y: 20,
            duration: 0.5,
            stagger: 0.1,
            ease: 'power2.out'
        });
    };

    // Notification/toast animations
    window.bmAnimateNotification = function(element) {
        gsap.from(element, {
            opacity: 0,
            y: -20,
            scale: 0.8,
            duration: 0.4,
            ease: 'back.out(1.7)'
        });
    };

    // Modal animations
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        const backdrop = modal.previousElementSibling;
        
        // Show modal
        modal.addEventListener('show.bs.modal', function() {
            gsap.from(backdrop, {
                opacity: 0,
                duration: 0.3
            });
            gsap.from(modal, {
                opacity: 0,
                scale: 0.8,
                y: 50,
                duration: 0.4,
                ease: 'back.out(1.7)'
            });
        });

        // Hide modal
        modal.addEventListener('hide.bs.modal', function() {
            gsap.to(modal, {
                opacity: 0,
                scale: 0.8,
                y: 50,
                duration: 0.3,
                ease: 'power2.in'
            });
            gsap.to(backdrop, {
                opacity: 0,
                duration: 0.3
            });
        });
    });

    // Dropdown animations
    const dropdowns = document.querySelectorAll('.dropdown-menu');
    dropdowns.forEach(menu => {
        const parent = menu.closest('.dropdown');
        if (!parent) return;

        const toggle = parent.querySelector('[data-bs-toggle="dropdown"]');
        if (!toggle) return;

        toggle.addEventListener('click', function() {
            if (menu.classList.contains('show')) {
                gsap.from(menu, {
                    opacity: 0,
                    y: -10,
                    duration: 0.3,
                    ease: 'power2.out'
                });
            }
        });
    });

    // Advanced animations using new plugins
    function initAdvancedAnimations() {
        // Text animations using TextPlugin
        if (typeof TextPlugin !== 'undefined') {
            const textElements = document.querySelectorAll('.section-title, h1, h2.animated-text');
            textElements.forEach(el => {
                const originalText = el.textContent;
                gsap.from(el, {
                    text: { value: '', delimiter: '' },
                    duration: 1,
                    ease: 'none',
                    scrollTrigger: {
                        trigger: el,
                        start: 'top 80%',
                        toggleActions: 'play none none none'
                    }
                });
            });
        }

        // Flip animations for layout changes
        if (typeof Flip !== 'undefined') {
            // Animate grid/list view transitions
            const viewToggleButtons = document.querySelectorAll('[data-view-toggle]');
            viewToggleButtons.forEach(btn => {
                btn.addEventListener('click', function() {
                    const container = document.querySelector(this.getAttribute('data-target'));
                    if (container) {
                        const state = Flip.getState(container.querySelectorAll('.movie-card, .card'));
                        // State will be captured, then after DOM change, animate
                        setTimeout(() => {
                            Flip.from(state, {
                                duration: 0.6,
                                ease: 'power2.inOut',
                                absolute: true
                            });
                        }, 10);
                    }
                });
            });
        }

        // Observer for touch/scroll gestures
        if (typeof Observer !== 'undefined') {
            Observer.create({
                target: window,
                type: 'scroll,pointer',
                onChangeY: (self) => {
                    // Add custom scroll-based animations here
                }
            });
        }

        // ScrollSmoother for smooth scrolling
        if (typeof ScrollSmoother !== 'undefined' && typeof ScrollTrigger !== 'undefined') {
            const smoother = ScrollSmoother.create({
                wrapper: '#smooth-wrapper',
                content: '#smooth-content',
                smooth: 1.5,
                effects: true,
                smoothTouch: 0.1
            });
            
            // Only enable if smooth-wrapper exists
            if (!document.getElementById('smooth-wrapper')) {
                // Create wrapper if needed
                const body = document.body;
                const wrapper = document.createElement('div');
                wrapper.id = 'smooth-wrapper';
                const content = document.createElement('div');
                content.id = 'smooth-content';
                
                // Move body content to wrapper (optional, can be done in HTML)
                // This is just for reference - actual implementation depends on layout
            }
        }

        // Draggable for interactive elements
        if (typeof Draggable !== 'undefined') {
            const draggableElements = document.querySelectorAll('[data-draggable]');
            draggableElements.forEach(el => {
                Draggable.create(el, {
                    type: 'x,y',
                    bounds: el.getAttribute('data-bounds') || window,
                    inertia: true,
                    onDrag: function() {
                        // Custom drag behavior
                    }
                });
            });
        }

        // MotionPath for animated paths
        if (typeof MotionPathPlugin !== 'undefined') {
            const pathElements = document.querySelectorAll('[data-motion-path]');
            pathElements.forEach(el => {
                const path = el.getAttribute('data-motion-path');
                gsap.to(el, {
                    motionPath: {
                        path: path,
                        autoRotate: true
                    },
                    duration: 2,
                    repeat: -1,
                    ease: 'none'
                });
            });
        }
    }

    console.log('GSAP Animations initialized with advanced plugins');
})();

