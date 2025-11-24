// GSAP Plugins Setup - Register all available plugins
// This file loads after gsap.min.js and registers all plugins

(function() {
    'use strict';

    // Wait for GSAP to be loaded
    if (typeof gsap === 'undefined') {
        console.warn('GSAP core library is not loaded');
        return;
    }

    // Register ScrollTrigger (most commonly used)
    if (typeof ScrollTrigger !== 'undefined') {
        gsap.registerPlugin(ScrollTrigger);
    }

    // Register ScrollToPlugin
    if (typeof ScrollToPlugin !== 'undefined') {
        gsap.registerPlugin(ScrollToPlugin);
    }

    // Register ScrollSmoother (requires ScrollTrigger)
    if (typeof ScrollSmoother !== 'undefined' && typeof ScrollTrigger !== 'undefined') {
        gsap.registerPlugin(ScrollSmoother);
    }

    // Register Flip
    if (typeof Flip !== 'undefined') {
        gsap.registerPlugin(Flip);
    }

    // Register Observer
    if (typeof Observer !== 'undefined') {
        gsap.registerPlugin(Observer);
    }

    // Register Draggable
    if (typeof Draggable !== 'undefined') {
        gsap.registerPlugin(Draggable);
    }

    // Register TextPlugin
    if (typeof TextPlugin !== 'undefined') {
        gsap.registerPlugin(TextPlugin);
    }

    // Register SplitText
    if (typeof SplitText !== 'undefined') {
        gsap.registerPlugin(SplitText);
    }

    // Register ScrambleTextPlugin
    if (typeof ScrambleTextPlugin !== 'undefined') {
        gsap.registerPlugin(ScrambleTextPlugin);
    }

    // Register MotionPathPlugin
    if (typeof MotionPathPlugin !== 'undefined') {
        gsap.registerPlugin(MotionPathPlugin);
    }

    // Register MorphSVGPlugin
    if (typeof MorphSVGPlugin !== 'undefined') {
        gsap.registerPlugin(MorphSVGPlugin);
    }

    // Register DrawSVGPlugin
    if (typeof DrawSVGPlugin !== 'undefined') {
        gsap.registerPlugin(DrawSVGPlugin);
    }

    // Register Physics2DPlugin
    if (typeof Physics2DPlugin !== 'undefined') {
        gsap.registerPlugin(Physics2DPlugin);
    }

    // Register PhysicsPropsPlugin
    if (typeof PhysicsPropsPlugin !== 'undefined') {
        gsap.registerPlugin(PhysicsPropsPlugin);
    }

    // Register InertiaPlugin
    if (typeof InertiaPlugin !== 'undefined') {
        gsap.registerPlugin(InertiaPlugin);
    }

    // Register PixiPlugin
    if (typeof PixiPlugin !== 'undefined') {
        gsap.registerPlugin(PixiPlugin);
    }

    // Register EaselPlugin
    if (typeof EaselPlugin !== 'undefined') {
        gsap.registerPlugin(EaselPlugin);
    }

    // Register CustomEase (if available)
    if (typeof CustomEase !== 'undefined') {
        gsap.registerPlugin(CustomEase);
    }

    // Register CustomBounce (if available)
    if (typeof CustomBounce !== 'undefined') {
        gsap.registerPlugin(CustomBounce);
    }

    // Register CustomWiggle (if available)
    if (typeof CustomWiggle !== 'undefined') {
        gsap.registerPlugin(CustomWiggle);
    }

    // Register SplitText (premium plugin)
    if (typeof SplitText !== 'undefined') {
        gsap.registerPlugin(SplitText);
    }

    // Register ScrambleTextPlugin (premium plugin)
    if (typeof ScrambleTextPlugin !== 'undefined') {
        gsap.registerPlugin(ScrambleTextPlugin);
    }

    // Register MorphSVGPlugin (premium plugin)
    if (typeof MorphSVGPlugin !== 'undefined') {
        gsap.registerPlugin(MorphSVGPlugin);
    }

    // Register DrawSVGPlugin (premium plugin)
    if (typeof DrawSVGPlugin !== 'undefined') {
        gsap.registerPlugin(DrawSVGPlugin);
    }

    // Register EasePack
    if (typeof EasePack !== 'undefined') {
        gsap.registerPlugin(EasePack);
    }

    // Register CSSRulePlugin
    if (typeof CSSRulePlugin !== 'undefined') {
        gsap.registerPlugin(CSSRulePlugin);
    }

    // Register GSDevTools (development tool)
    if (typeof GSDevTools !== 'undefined') {
        gsap.registerPlugin(GSDevTools);
    }

    // Register MotionPathHelper
    if (typeof MotionPathHelper !== 'undefined') {
        gsap.registerPlugin(MotionPathHelper);
    }

    console.log('GSAP plugins registered');
})();

