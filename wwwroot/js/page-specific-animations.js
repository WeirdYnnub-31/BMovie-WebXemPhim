// Page-specific GSAP animations for BMovie
(function() {
    'use strict';

    if (typeof gsap === 'undefined') {
        console.warn('GSAP is not loaded');
        return;
    }

    // Get current page path
    const currentPath = window.location.pathname.toLowerCase();
    const currentPage = currentPath.split('/').filter(p => p).join('-') || 'home';

    // Initialize page-specific animations when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        // Apply animations based on current page
        switch(true) {
            case currentPath.includes('/phim-le'):
            case currentPath.includes('/phim-bo'):
                initMoviesIndexAnimations();
                break;
            case currentPath.includes('/movies/explorer'):
            case currentPath.includes('/khampha'):
                initExplorerAnimations();
                break;
            case currentPath.includes('/dangnhap'):
            case currentPath.includes('/login'):
                initLoginAnimations();
                break;
            case currentPath.includes('/dangky'):
            case currentPath.includes('/register'):
                initRegisterAnimations();
                break;
            case currentPath.includes('/admin'):
                initAdminAnimations();
                break;
            case currentPath.includes('/profile'):
            case currentPath.includes('/Profile'):
                initProfileAnimations();
                break;
            case currentPath.includes('/movie/'):
            case currentPath.includes('/movies/'):
                initMovieDetailAnimations();
                break;
            case currentPath === '/' || currentPath === '':
                initHomeAnimations();
                break;
            default:
                initDefaultPageAnimations();
        }
    });

    // Movies Index Page (phim-le, phim-bo)
    function initMoviesIndexAnimations() {
        // Animate section title
        const sectionTitle = document.querySelector('.section-title');
        if (sectionTitle) {
            gsap.from(sectionTitle, {
                opacity: 0,
                y: -30,
                duration: 0.8,
                ease: 'power3.out'
            });
        }

        // Animate movie cards with stagger
        const movieCards = document.querySelectorAll('.movie-card');
        if (movieCards.length > 0) {
            gsap.from(movieCards, {
                opacity: 0,
                y: 50,
                scale: 0.9,
                duration: 0.6,
                stagger: {
                    amount: 0.8,
                    from: 'start'
                },
                scrollTrigger: {
                    trigger: movieCards[0]?.closest('.row') || document.body,
                    start: 'top 80%',
                    toggleActions: 'play none none none'
                },
                ease: 'back.out(1.2)'
            });

            // Enhanced hover effects
            movieCards.forEach((card, index) => {
                card.addEventListener('mouseenter', function() {
                    gsap.to(card, {
                        y: -10,
                        scale: 1.05,
                        rotation: 0,
                        boxShadow: '0 15px 40px rgba(0, 0, 0, 0.6)',
                        duration: 0.4,
                        ease: 'power2.out'
                    });
                    
                    // Animate overlay
                    const overlay = card.querySelector('.overlay');
                    if (overlay) {
                        gsap.to(overlay, {
                            opacity: 1,
                            duration: 0.3
                        });
                    }
                });

                card.addEventListener('mouseleave', function() {
                    gsap.to(card, {
                        y: 0,
                        scale: 1,
                        rotation: 0,
                        boxShadow: '0 0 0 rgba(0, 0, 0, 0)',
                        duration: 0.4,
                        ease: 'power2.out'
                    });
                    
                    const overlay = card.querySelector('.overlay');
                    if (overlay) {
                        gsap.to(overlay, {
                            opacity: 0,
                            duration: 0.3
                        });
                    }
                });
            });
        }

        // Animate MovieFilter component
        const movieFilter = document.querySelector('[class*="movie-filter"]');
        if (movieFilter) {
            gsap.from(movieFilter, {
                opacity: 0,
                y: 20,
                duration: 0.6,
                delay: 0.2,
                ease: 'power2.out'
            });
        }
    }

    // Explorer Page (khampha)
    function initExplorerAnimations() {
        // Animate explorer wrapper
        const explorer = document.querySelector('.movies-explorer-wrapper, .movies-explorer');
        if (explorer) {
            gsap.from(explorer, {
                opacity: 0,
                scale: 0.95,
                duration: 0.8,
                ease: 'power3.out'
            });
        }

        // Animate movie grid items
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) {
                        const cards = node.querySelectorAll ? node.querySelectorAll('.movie-card, [class*="movie-card"]') : [];
                        if (cards.length > 0) {
                            gsap.from(cards, {
                                opacity: 0,
                                y: 30,
                                scale: 0.9,
                                duration: 0.5,
                                stagger: 0.1,
                                ease: 'back.out(1.2)'
                            });
                        }
                    }
                });
            });
        });

        const targetNode = document.querySelector('.movies-explorer-wrapper, .movies-grid');
        if (targetNode) {
            observer.observe(targetNode, {
                childList: true,
                subtree: true
            });
        }

        // Animate filters
        const filters = document.querySelectorAll('[class*="filter"], .form-control, select');
        gsap.from(filters, {
            opacity: 0,
            x: -20,
            duration: 0.5,
            stagger: 0.1,
            delay: 0.3,
            ease: 'power2.out'
        });
    }

    // Login Page
    function initLoginAnimations() {
        const form = document.querySelector('form');
        const glassCard = document.querySelector('.glass-card');
        
        if (glassCard) {
            gsap.from(glassCard, {
                opacity: 0,
                scale: 0.9,
                y: 50,
                duration: 0.8,
                ease: 'back.out(1.7)'
            });
        }

        if (form) {
            const inputs = form.querySelectorAll('input, button');
            gsap.from(inputs, {
                opacity: 0,
                y: 20,
                duration: 0.5,
                stagger: 0.1,
                delay: 0.3,
                ease: 'power2.out'
            });
        }

        // Animate title
        const title = document.querySelector('.section-title');
        if (title) {
            if (typeof TextPlugin !== 'undefined') {
                gsap.from(title, {
                    text: { value: '', delimiter: '' },
                    duration: 1,
                    ease: 'none'
                });
            } else {
                gsap.from(title, {
                    opacity: 0,
                    y: -20,
                    duration: 0.6,
                    ease: 'power2.out'
                });
            }
        }

        // Social login buttons animation
        const socialButtons = document.querySelectorAll('.btn-outline-light');
        gsap.from(socialButtons, {
            opacity: 0,
            scale: 0.8,
            duration: 0.4,
            stagger: 0.1,
            delay: 0.6,
            ease: 'back.out(1.7)'
        });
    }

    // Register Page
    function initRegisterAnimations() {
        const form = document.querySelector('form');
        const glassCard = document.querySelector('.glass-card');
        
        if (glassCard) {
            gsap.from(glassCard, {
                opacity: 0,
                scale: 0.9,
                y: 50,
                rotation: -2,
                duration: 0.8,
                ease: 'back.out(1.7)'
            });
        }

        if (form) {
            const formGroups = form.querySelectorAll('.mb-3');
            gsap.from(formGroups, {
                opacity: 0,
                x: -30,
                duration: 0.5,
                stagger: 0.15,
                delay: 0.3,
                ease: 'power2.out'
            });

            // Submit button special animation
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                gsap.from(submitBtn, {
                    opacity: 0,
                    scale: 0.5,
                    rotation: -180,
                    duration: 0.8,
                    delay: 0.9,
                    ease: 'back.out(2)'
                });
            }
        }

        // Error messages animation
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            gsap.from(alert, {
                opacity: 0,
                x: -50,
                scale: 0.8,
                duration: 0.5,
                ease: 'back.out(1.7)'
            });
        });
    }

    // Admin Page
    function initAdminAnimations() {
        // Animate sidebar
        const sidebar = document.querySelector('.admin-sidebar-nav, [class*="sidebar"], .col-lg-3');
        if (sidebar) {
            gsap.from(sidebar, {
                opacity: 0,
                x: -50,
                duration: 0.6,
                ease: 'power2.out'
            });
            
            // Animate sidebar items
            const sidebarItems = sidebar.querySelectorAll('.sidebar-item, a, button');
            if (sidebarItems.length > 0) {
                gsap.from(sidebarItems, {
                    opacity: 0,
                    x: -20,
                    duration: 0.4,
                    stagger: 0.05,
                    delay: 0.3,
                    ease: 'power2.out'
                });
            }
        }

        // Animate main content
        const mainContent = document.querySelector('main, .col-lg-9');
        if (mainContent) {
            gsap.from(mainContent, {
                opacity: 0,
                x: 50,
                duration: 0.6,
                delay: 0.2,
                ease: 'power2.out'
            });
        }

        // Animate section titles
        const sectionTitles = document.querySelectorAll('.section-title');
        gsap.from(sectionTitles, {
            opacity: 0,
            y: -20,
            duration: 0.6,
            stagger: 0.1,
            delay: 0.3,
            ease: 'power2.out'
        });

        // Animate statistics cards with counter effect
        const statCards = document.querySelectorAll('.glass-card.p-3');
        statCards.forEach((card, index) => {
            gsap.from(card, {
                opacity: 0,
                scale: 0.8,
                y: 30,
                rotation: -5,
                duration: 0.6,
                delay: 0.4 + (index * 0.1),
                ease: 'back.out(1.7)',
                onComplete: () => {
                    // Animate numbers counting up
                    const numberEl = card.querySelector('.fs-3, .h4, .fs-2');
                    if (numberEl && typeof TextPlugin !== 'undefined') {
                        const finalValue = numberEl.textContent.trim();
                        const numValue = parseFloat(finalValue.replace(/[^0-9.]/g, ''));
                        if (!isNaN(numValue)) {
                            gsap.from(numberEl, {
                                text: { value: '0', delimiter: '' },
                                duration: 1,
                                ease: 'power1.out'
                            });
                        }
                    }
                }
            });

            // Add hover effects
            card.addEventListener('mouseenter', function() {
                gsap.to(card, {
                    y: -5,
                    scale: 1.02,
                    boxShadow: '0 10px 30px rgba(162, 89, 217, 0.3)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
            });

            card.addEventListener('mouseleave', function() {
                gsap.to(card, {
                    y: 0,
                    scale: 1,
                    boxShadow: '0 0 0 rgba(0, 0, 0, 0)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
            });
        });

        // Animate quick action cards
        const quickActionCards = document.querySelectorAll('a .glass-card');
        gsap.from(quickActionCards, {
            opacity: 0,
            scale: 0.9,
            y: 20,
            duration: 0.5,
            stagger: 0.08,
            delay: 0.6,
            ease: 'back.out(1.5)'
        });

        quickActionCards.forEach(card => {
            card.addEventListener('mouseenter', function() {
                gsap.to(card, {
                    y: -8,
                    scale: 1.05,
                    rotation: 2,
                    boxShadow: '0 15px 40px rgba(162, 89, 217, 0.4)',
                    duration: 0.3,
                    ease: 'back.out(1.7)'
                });
                
                const icon = card.querySelector('.fs-3');
                if (icon) {
                    gsap.to(icon, {
                        scale: 1.2,
                        rotation: 360,
                        duration: 0.5,
                        ease: 'back.out(1.7)'
                    });
                }
            });

            card.addEventListener('mouseleave', function() {
                gsap.to(card, {
                    y: 0,
                    scale: 1,
                    rotation: 0,
                    boxShadow: '0 0 0 rgba(0, 0, 0, 0)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
                
                const icon = card.querySelector('.fs-3');
                if (icon) {
                    gsap.to(icon, {
                        scale: 1,
                        rotation: 0,
                        duration: 0.3,
                        ease: 'power2.out'
                    });
                }
            });
        });

        // Animate charts when they load
        const chartContainers = document.querySelectorAll('canvas');
        chartContainers.forEach((canvas, index) => {
            const container = canvas.closest('.glass-card');
            if (container) {
                gsap.from(container, {
                    opacity: 0,
                    scale: 0.95,
                    y: 30,
                    duration: 0.8,
                    delay: 0.8 + (index * 0.15),
                    ease: 'power3.out',
                    scrollTrigger: {
                        trigger: container,
                        start: 'top 80%',
                        toggleActions: 'play none none none'
                    }
                });
            }
        });

        // Animate tables
        const tables = document.querySelectorAll('table');
        tables.forEach((table, index) => {
            gsap.from(table, {
                opacity: 0,
                y: 30,
                duration: 0.6,
                delay: 0.9 + (index * 0.1),
                ease: 'power2.out',
                scrollTrigger: {
                    trigger: table,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                }
            });

            // Animate table rows
            const rows = table.querySelectorAll('tbody tr');
            gsap.from(rows, {
                opacity: 0,
                x: -30,
                duration: 0.4,
                stagger: 0.05,
                delay: 1.2 + (index * 0.1),
                ease: 'power2.out'
            });
        });

        // Animate top movies list
        const topMoviesList = document.querySelectorAll('ol li, ul li');
        if (topMoviesList.length > 0) {
            gsap.from(topMoviesList, {
                opacity: 0,
                x: -20,
                duration: 0.4,
                stagger: 0.05,
                delay: 0.7,
                ease: 'power2.out'
            });
        }

        // Add pulse animation to important numbers
        const importantNumbers = document.querySelectorAll('.fs-2.fw-bold, .fs-3.fw-bold');
        importantNumbers.forEach(num => {
            gsap.to(num, {
                scale: 1.1,
                duration: 0.3,
                yoyo: true,
                repeat: 1,
                delay: 1.5,
                ease: 'power2.inOut'
            });
        });
    }

    // Home Page
    function initHomeAnimations() {
        // Hero section animation
        const heroSection = document.getElementById('hero-section');
        if (heroSection) {
            gsap.from(heroSection, {
                opacity: 0,
                scale: 1.1,
                duration: 1,
                ease: 'power2.out'
            });

            // Hero content
            const heroContent = heroSection.querySelector('.hero-content');
            if (heroContent) {
                gsap.from(heroContent, {
                    opacity: 0,
                    y: 50,
                    duration: 1,
                    delay: 0.3,
                    ease: 'power3.out'
                });
            }

            // Hero title
            const heroTitle = heroSection.querySelector('.hero-title');
            if (heroTitle) {
                if (typeof TextPlugin !== 'undefined') {
                    gsap.from(heroTitle, {
                        text: { value: '', delimiter: ' ' },
                        duration: 1.5,
                        delay: 0.5,
                        ease: 'none'
                    });
                } else {
                    gsap.from(heroTitle, {
                        opacity: 0,
                        y: 30,
                        duration: 1,
                        delay: 0.5,
                        ease: 'power3.out'
                    });
                }
            }

            // Hero buttons - FIX: Ensure buttons are always visible
            const heroButtons = heroSection.querySelectorAll('.hero-buttons .btn, .hero-btn-primary, .hero-btn-secondary');
            if (heroButtons.length > 0) {
                // CRITICAL: Set initial state to visible
                heroButtons.forEach(btn => {
                    btn.style.opacity = '1';
                    btn.style.visibility = 'visible';
                    gsap.set(btn, { opacity: 1, visibility: 'visible' });
                });
                
                // Then animate from 0
                gsap.from(heroButtons, {
                    opacity: 0,
                    scale: 0.8,
                    y: 20,
                    duration: 0.6,
                    stagger: 0.2,
                    delay: 0.8,
                    ease: 'back.out(1.7)',
                    onComplete: function() {
                        // CRITICAL: Force visible after animation completes
                        heroButtons.forEach(btn => {
                            btn.style.opacity = '1';
                            btn.style.visibility = 'visible';
                            gsap.set(btn, { opacity: 1, visibility: 'visible', clearProps: 'all' });
                        });
                    }
                });
                
                // Also ensure visibility after a delay in case animation doesn't complete
                setTimeout(() => {
                    heroButtons.forEach(btn => {
                        btn.style.setProperty('opacity', '1', 'important');
                        btn.style.setProperty('visibility', 'visible', 'important');
                        gsap.set(btn, { opacity: 1, visibility: 'visible', clearProps: 'all' });
                    });
                }, 2000);
            }

            // Parallax effect for hero background
            if (typeof ScrollTrigger !== 'undefined') {
                const heroBg = heroSection.querySelector('.hero-bg');
                if (heroBg) {
                    gsap.to(heroBg, {
                        y: -100,
                        scale: 1.1,
                        scrollTrigger: {
                            trigger: heroSection,
                            start: 'top top',
                            end: 'bottom top',
                            scrub: true
                        }
                    });
                }
            }
        }

        // Movie sections animation - ensure visible first, then animate if GSAP available
        const movieSections = document.querySelectorAll('.movie-section, .movie-section-modern');
        if (movieSections.length > 0) {
            movieSections.forEach((section, index) => {
                // CRITICAL: Always ensure section is visible first - set inline styles
                section.style.opacity = '1';
                section.style.transform = 'translateY(0)';
                section.style.visibility = 'visible';
                section.style.display = '';
                gsap.set(section, { opacity: 1, y: 0 });
                
                // Only animate if ScrollTrigger is available
                if (typeof ScrollTrigger !== 'undefined') {
                    // Then animate on scroll
                    gsap.from(section, {
                        opacity: 0,
                        y: 50,
                        duration: 0.8,
                        scrollTrigger: {
                            trigger: section,
                            start: 'top 85%',
                            toggleActions: 'play none none none'
                        },
                        delay: index * 0.1,
                        ease: 'power3.out'
                    });
                }

            // Section title - ensure visible first
            const sectionTitle = section.querySelector('.movie-section-title');
            if (sectionTitle) {
                gsap.set(sectionTitle, { opacity: 1, x: 0 });
                
                if (typeof ScrollTrigger !== 'undefined') {
                    gsap.from(sectionTitle, {
                        opacity: 0,
                        x: -30,
                        duration: 0.6,
                        scrollTrigger: {
                            trigger: section,
                            start: 'top 85%',
                            toggleActions: 'play none none none'
                        },
                        delay: 0.2,
                        ease: 'power2.out'
                    });
                }
            }

            // Movie scroll items - ensure visible first
            const scrollItems = section.querySelectorAll('.movie-scroll-item');
            if (scrollItems.length > 0) {
                // CRITICAL: Set inline styles to ensure visibility
                scrollItems.forEach(item => {
                    item.style.opacity = '1';
                    item.style.transform = 'translateX(0) scale(1)';
                    item.style.visibility = 'visible';
                    item.style.display = '';
                });
                // Set initial state to visible with GSAP
                gsap.set(scrollItems, { opacity: 1, x: 0, scale: 1 });
                
                // Then animate on scroll if ScrollTrigger is available
                if (typeof ScrollTrigger !== 'undefined') {
                    gsap.from(scrollItems, {
                        opacity: 0,
                        x: 50,
                        scale: 0.9,
                        duration: 0.5,
                        stagger: 0.1,
                        scrollTrigger: {
                            trigger: section,
                            start: 'top 85%',
                            toggleActions: 'play none none none'
                        },
                        ease: 'back.out(1.2)'
                    });
                }
            }
            });
        }

        // Recommendations section
        const recommendationsSection = document.querySelector('.recommendations-special');
        if (recommendationsSection) {
            gsap.from(recommendationsSection, {
                opacity: 0,
                y: 50,
                duration: 0.8,
                scrollTrigger: {
                    trigger: recommendationsSection,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power3.out'
            });
        }
    }

    // Movie Detail Page
    function initMovieDetailAnimations() {
        // Hero section with poster and info
        const movieSection = document.querySelector('section.mb-5');
        if (movieSection) {
            gsap.from(movieSection, {
                opacity: 0,
                y: 30,
                duration: 0.8,
                ease: 'power3.out'
            });
        }

        // Poster animation
        const poster = document.querySelector('.movie-card .poster, .poster');
        if (poster) {
            gsap.from(poster, {
                opacity: 0,
                scale: 0.8,
                rotation: -5,
                duration: 1,
                delay: 0.2,
                ease: 'back.out(1.7)'
            });

            // Hover effect for poster
            poster.addEventListener('mouseenter', function() {
                gsap.to(poster, {
                    scale: 1.05,
                    rotation: 2,
                    duration: 0.4,
                    ease: 'power2.out'
                });
            });

            poster.addEventListener('mouseleave', function() {
                gsap.to(poster, {
                    scale: 1,
                    rotation: 0,
                    duration: 0.4,
                    ease: 'power2.out'
                });
            });
        }

        // Movie title animation
        const movieTitle = document.querySelector('h1.h3, h1');
        if (movieTitle) {
            if (typeof TextPlugin !== 'undefined') {
                const originalText = movieTitle.textContent;
                gsap.from(movieTitle, {
                    text: { value: '', delimiter: ' ' },
                    duration: 1.2,
                    delay: 0.3,
                    ease: 'none'
                });
            } else {
                gsap.from(movieTitle, {
                    opacity: 0,
                    y: -30,
                    duration: 0.8,
                    delay: 0.3,
                    ease: 'power3.out'
                });
            }
        }

        // Badges animation
        const badges = document.querySelectorAll('.badge, .tag');
        if (badges.length > 0) {
            gsap.from(badges, {
                opacity: 0,
                scale: 0,
                rotation: -180,
                duration: 0.5,
                stagger: 0.1,
                delay: 0.5,
                ease: 'back.out(2)'
            });
        }

        // Description animation
        const description = document.querySelector('p.text-white-50');
        if (description) {
            gsap.from(description, {
                opacity: 0,
                y: 20,
                duration: 0.6,
                delay: 0.7,
                ease: 'power2.out'
            });
        }

        // Star rating animation
        const starRating = document.querySelector('[class*="star"], [class*="rating"]');
        if (starRating) {
            gsap.from(starRating, {
                opacity: 0,
                scale: 0.5,
                rotation: -180,
                duration: 0.8,
                delay: 0.8,
                ease: 'back.out(2)'
            });
        }

        // Action buttons animation
        const actionButtons = document.querySelectorAll('.neon-btn, .btn-outline-light, .dropdown');
        if (actionButtons.length > 0) {
            gsap.from(actionButtons, {
                opacity: 0,
                y: 20,
                scale: 0.9,
                duration: 0.5,
                stagger: 0.1,
                delay: 0.9,
                ease: 'back.out(1.7)'
            });

            // Enhanced hover effects
            actionButtons.forEach(btn => {
                btn.addEventListener('mouseenter', function() {
                    gsap.to(btn, {
                        scale: 1.05,
                        y: -2,
                        duration: 0.3,
                        ease: 'power2.out'
                    });
                });

                btn.addEventListener('mouseleave', function() {
                    gsap.to(btn, {
                        scale: 1,
                        y: 0,
                        duration: 0.3,
                        ease: 'power2.out'
                    });
                });
            });
        }

        // Comments section animation
        const commentsSection = document.querySelector('section.mb-5:last-of-type');
        if (commentsSection) {
            gsap.from(commentsSection, {
                opacity: 0,
                y: 50,
                duration: 0.8,
                scrollTrigger: {
                    trigger: commentsSection,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power3.out'
            });
        }

        // Comment form animation
        const commentForm = document.querySelector('form[method="post"][asp-controller="Comments"]');
        if (commentForm) {
            gsap.from(commentForm, {
                opacity: 0,
                x: -30,
                duration: 0.6,
                scrollTrigger: {
                    trigger: commentForm,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power2.out'
            });
        }

        // Comments list animation
        const commentsList = document.querySelectorAll('.comments-list > *, [class*="comment"]');
        if (commentsList.length > 0) {
            gsap.from(commentsList, {
                opacity: 0,
                x: -20,
                duration: 0.5,
                stagger: 0.1,
                scrollTrigger: {
                    trigger: commentsList[0] || document.body,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power2.out'
            });
        }

        // Sidebar animation
        const sidebar = document.querySelector('aside.col-lg-3, aside');
        if (sidebar) {
            gsap.from(sidebar, {
                opacity: 0,
                x: 50,
                duration: 0.8,
                delay: 0.4,
                ease: 'power2.out'
            });

            // Sidebar items animation
            const sidebarItems = sidebar.querySelectorAll('a.d-flex');
            if (sidebarItems.length > 0) {
                gsap.from(sidebarItems, {
                    opacity: 0,
                    x: 30,
                    duration: 0.5,
                    stagger: 0.1,
                    delay: 0.6,
                    ease: 'power2.out'
                });

                // Hover effects for sidebar items
                sidebarItems.forEach(item => {
                    item.addEventListener('mouseenter', function() {
                        gsap.to(item, {
                            x: 5,
                            scale: 1.02,
                            duration: 0.3,
                            ease: 'power2.out'
                        });
                    });

                    item.addEventListener('mouseleave', function() {
                        gsap.to(item, {
                            x: 0,
                            scale: 1,
                            duration: 0.3,
                            ease: 'power2.out'
                        });
                    });
                });
            }
        }

        // Modal animation (trailer modal)
        const trailerModal = document.getElementById('trailerModal');
        if (trailerModal) {
            trailerModal.addEventListener('show.bs.modal', function() {
                const modalContent = this.querySelector('.modal-content');
                if (modalContent) {
                    gsap.from(modalContent, {
                        opacity: 0,
                        scale: 0.8,
                        y: 50,
                        rotation: -5,
                        duration: 0.5,
                        ease: 'back.out(1.7)'
                    });
                }
            });
        }

        // Glass card animations
        const glassCards = document.querySelectorAll('.glass-card');
        glassCards.forEach((card, index) => {
            gsap.from(card, {
                opacity: 0,
                y: 20,
                scale: 0.95,
                duration: 0.6,
                delay: 0.2 + (index * 0.1),
                ease: 'power2.out'
            });
        });

        // Section titles animation
        const sectionTitles = document.querySelectorAll('.section-title, h2');
        sectionTitles.forEach(title => {
            gsap.from(title, {
                opacity: 0,
                x: -30,
                duration: 0.6,
                scrollTrigger: {
                    trigger: title,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power2.out'
            });
        });
    }

    // Default page animations
    // Profile Page
    function initProfileAnimations() {
        // Ensure all elements are visible first
        const profileElements = document.querySelectorAll('.profile-header-card, .profile-avatar-wrapper, .profile-stat-card, .profile-info-card, .profile-settings-card');
        gsap.set(profileElements, { opacity: 1, y: 0, x: 0, scale: 1, rotation: 0 });

        // Animate profile header
        const profileHeader = document.querySelector('.profile-header-card');
        if (profileHeader) {
            gsap.from(profileHeader, {
                opacity: 0,
                y: -30,
                scale: 0.95,
                duration: 0.8,
                ease: 'back.out(1.7)'
            });
        }

        // Animate avatar
        const avatar = document.querySelector('.profile-avatar-wrapper');
        if (avatar) {
            gsap.from(avatar, {
                opacity: 0,
                scale: 0.5,
                rotation: -180,
                duration: 1,
                delay: 0.2,
                ease: 'back.out(2)'
            });
        }

        // Animate username
        const username = document.querySelector('.profile-username');
        if (username) {
            if (typeof TextPlugin !== 'undefined') {
                gsap.from(username, {
                    text: { value: '', delimiter: '' },
                    duration: 1.2,
                    delay: 0.4,
                    ease: 'none'
                });
            } else {
                gsap.from(username, {
                    opacity: 0,
                    x: -30,
                    duration: 0.8,
                    delay: 0.4,
                    ease: 'power3.out'
                });
            }
        }

        // Animate stat cards - ensure visible first
        const statCards = document.querySelectorAll('.profile-stat-card');
        if (statCards.length > 0) {
            gsap.set(statCards, { opacity: 1, scale: 1, y: 0 });
            
            if (typeof ScrollTrigger !== 'undefined') {
                gsap.from(statCards, {
                    opacity: 0,
                    scale: 0.8,
                    y: 30,
                    duration: 0.6,
                    stagger: 0.1,
                    delay: 0.6,
                    ease: 'back.out(1.5)',
                    scrollTrigger: {
                        trigger: statCards[0],
                        start: 'top 85%',
                        toggleActions: 'play none none none'
                    }
                });
            }
        }

        // Add hover effects to stat cards
        statCards.forEach(card => {
            card.addEventListener('mouseenter', function() {
                gsap.to(card, {
                    y: -8,
                    scale: 1.05,
                    boxShadow: '0 15px 40px rgba(162, 89, 217, 0.4)',
                    duration: 0.3,
                    ease: 'back.out(1.7)'
                });
            });

            card.addEventListener('mouseleave', function() {
                gsap.to(card, {
                    y: 0,
                    scale: 1,
                    boxShadow: '0 0 0 rgba(0, 0, 0, 0)',
                    duration: 0.3,
                    ease: 'power2.out'
                });
            });
        });

        // Animate info and settings cards - ensure visible first
        const infoCards = document.querySelectorAll('.profile-info-card, .profile-settings-card');
        if (infoCards.length > 0) {
            gsap.set(infoCards, { opacity: 1, x: 0 });
            
            if (typeof ScrollTrigger !== 'undefined') {
                gsap.from(infoCards, {
                    opacity: 0,
                    x: 50,
                    duration: 0.8,
                    stagger: 0.2,
                    delay: 0.8,
                    ease: 'power3.out',
                    scrollTrigger: {
                        trigger: infoCards[0],
                        start: 'top 85%',
                        toggleActions: 'play none none none'
                    }
                });
            }
        }

        // Animate form inputs
        const formInputs = document.querySelectorAll('.profile-form input');
        gsap.from(formInputs, {
            opacity: 0,
            y: 20,
            duration: 0.5,
            stagger: 0.1,
            delay: 1,
            ease: 'power2.out'
        });

        // Animate buttons
        const buttons = document.querySelectorAll('.profile-actions .btn, .profile-form button');
        gsap.from(buttons, {
            opacity: 0,
            scale: 0.8,
            duration: 0.5,
            stagger: 0.1,
            delay: 1.2,
            ease: 'back.out(1.7)'
        });

        // Animate avatar badge and make it clickable
        const avatarBadge = document.querySelector('.profile-avatar-badge');
        if (avatarBadge) {
            const avatarInput = document.getElementById('avatarInput');
            if (avatarInput) {
                avatarBadge.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    avatarInput.click();
                });
            }
            
            gsap.to(avatarBadge, {
                scale: 1.1,
                duration: 0.3,
                yoyo: true,
                repeat: 1,
                delay: 1.5,
                ease: 'power2.inOut'
            });
        }

        // Animate alert if present
        const alert = document.querySelector('.profile-alert');
        if (alert) {
            gsap.from(alert, {
                opacity: 0,
                y: -20,
                scale: 0.9,
                duration: 0.5,
                ease: 'back.out(1.7)'
            });
        }

        // Animate info items
        const infoItems = document.querySelectorAll('.profile-info-item');
        gsap.from(infoItems, {
            opacity: 0,
            x: -20,
            duration: 0.5,
            stagger: 0.1,
            delay: 1,
            ease: 'power2.out'
        });
    }

    function initDefaultPageAnimations() {
        // Animate all sections
        const sections = document.querySelectorAll('section, .section');
        sections.forEach((section, index) => {
            gsap.from(section, {
                opacity: 0,
                y: 30,
                duration: 0.6,
                scrollTrigger: {
                    trigger: section,
                    start: 'top 80%',
                    toggleActions: 'play none none none'
                },
                delay: index * 0.1,
                ease: 'power2.out'
            });
        });

        // Animate headings
        const headings = document.querySelectorAll('h1, h2, h3, .section-title');
        headings.forEach(heading => {
            gsap.from(heading, {
                opacity: 0,
                y: -20,
                duration: 0.6,
                scrollTrigger: {
                    trigger: heading,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                ease: 'power2.out'
            });
        });
    }

    // Enhanced badge animations
    function initBadgeAnimations() {
        const badges = document.querySelectorAll('.badge, .tag');
        badges.forEach((badge, index) => {
            gsap.from(badge, {
                opacity: 0,
                scale: 0,
                rotation: -180,
                duration: 0.5,
                delay: index * 0.05,
                scrollTrigger: {
                    trigger: badge,
                    start: 'top 90%',
                    toggleActions: 'play none none none'
                },
                ease: 'back.out(2)'
            });
        });
    }

    // Enhanced button group animations
    function initButtonGroupAnimations() {
        const buttonGroups = document.querySelectorAll('.d-flex.gap-2, .btn-group');
        buttonGroups.forEach(group => {
            const buttons = group.querySelectorAll('.btn, button');
            gsap.from(buttons, {
                opacity: 0,
                y: 20,
                scale: 0.8,
                duration: 0.4,
                stagger: 0.1,
                ease: 'back.out(1.7)'
            });
        });
    }

    // Loading state animations
    function initLoadingAnimations() {
        const loadingElements = document.querySelectorAll('.spinner-border, .loading, [class*="loading"]');
        loadingElements.forEach(loader => {
            gsap.to(loader, {
                rotation: 360,
                duration: 1,
                repeat: -1,
                ease: 'none'
            });
        });
    }

    // Form validation animations
    function initFormValidationAnimations() {
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.addEventListener('submit', function(e) {
                const invalidInputs = form.querySelectorAll(':invalid');
                if (invalidInputs.length > 0) {
                    gsap.to(invalidInputs, {
                        x: [-10, 10, -10, 10, 0],
                        duration: 0.5,
                        ease: 'power2.out'
                    });
                }
            });
        });
    }

    // Initialize additional animations
    setTimeout(() => {
        initBadgeAnimations();
        initButtonGroupAnimations();
        initLoadingAnimations();
        initFormValidationAnimations();
    }, 500);

    // Smooth page transitions
    document.addEventListener('click', function(e) {
        const link = e.target.closest('a[href]');
        if (link && link.href && !link.target && !link.hasAttribute('download')) {
            const href = link.getAttribute('href');
            if (href.startsWith('/') || href.startsWith(window.location.origin)) {
                // Add fade out animation before navigation
                gsap.to('body', {
                    opacity: 0,
                    duration: 0.3,
                    ease: 'power2.in'
                });
            }
        }
    });

    // Restore opacity on page load
    gsap.set('body', { opacity: 1 });

    console.log(`GSAP page-specific animations initialized for: ${currentPage}`);
})();

