// Enhanced Video Player với Resume Watching, HLS.js, Quality Selector, PiP
(function() {
    'use strict';

    const movieId = parseInt(document.getElementById('movie-id')?.value || '0');
    const episodeNumber = parseInt(document.getElementById('episode-number')?.value || '0') || null;
    const video = document.getElementById('main-video');
    if (!video || movieId === 0) return;

    let hls = null;
    let progressSaveInterval = null;
    let lastSavedTime = 0;
    const SAVE_INTERVAL = 10000; // Lưu progress mỗi 10 giây

    // Initialize HLS.js nếu URL là HLS
    function initHLS(videoUrl) {
        if (videoUrl.includes('.m3u8') || videoUrl.includes('hls')) {
            if (typeof Hls !== 'undefined' && Hls.isSupported()) {
                if (hls) {
                    hls.destroy();
                }
                hls = new Hls({
                    enableWorker: true,
                    lowLatencyMode: false,
                    backBufferLength: 90
                });
                hls.loadSource(videoUrl);
                hls.attachMedia(video);
                
                // Quality levels
                hls.on(Hls.Events.MANIFEST_PARSED, function() {
                    const levels = hls.levels;
                    createQualitySelector(levels);
                });

                // Error handling
                hls.on(Hls.Events.ERROR, function(event, data) {
                    if (data.fatal) {
                        switch(data.type) {
                            case Hls.ErrorTypes.NETWORK_ERROR:
                                console.error('Network error, trying to recover...');
                                hls.startLoad();
                                break;
                            case Hls.ErrorTypes.MEDIA_ERROR:
                                console.error('Media error, trying to recover...');
                                hls.recoverMediaError();
                                break;
                            default:
                                console.error('Fatal error, destroying HLS...');
                                hls.destroy();
                                break;
                        }
                    }
                });
            } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
                // Native HLS support (Safari)
                video.src = videoUrl;
            }
        } else {
            // Regular MP4
            if (hls) {
                hls.destroy();
                hls = null;
            }
            video.src = videoUrl;
        }
    }

    // Create quality selector
    function createQualitySelector(levels) {
        const existingSelector = document.getElementById('quality-selector');
        if (existingSelector) {
            existingSelector.remove();
        }

        if (!levels || levels.length <= 1) return;

        const selector = document.createElement('div');
        selector.id = 'quality-selector';
        selector.className = 'quality-selector';
        selector.innerHTML = '<label>Chất lượng: </label><select id="quality-select"></select>';

        const select = selector.querySelector('select');
        
        // Add auto option
        const autoOption = document.createElement('option');
        autoOption.value = 'auto';
        autoOption.textContent = 'Tự động';
        autoOption.selected = true;
        select.appendChild(autoOption);

        // Add quality options
        levels.forEach((level, index) => {
            const option = document.createElement('option');
            option.value = index;
            const height = level.height || '?';
            const bitrate = level.bitrate ? Math.round(level.bitrate / 1000) + 'k' : '';
            option.textContent = `${height}p${bitrate ? ' (' + bitrate + ')' : ''}`;
            select.appendChild(option);
        });

        // Insert before video controls
        const playerWrap = document.getElementById('player-wrap');
        if (playerWrap) {
            const controls = playerWrap.querySelector('.video-controls-overlay') || playerWrap;
            playerWrap.insertBefore(selector, controls);
        }

        select.addEventListener('change', function() {
            if (hls && this.value !== 'auto') {
                hls.currentLevel = parseInt(this.value);
            } else if (hls) {
                hls.currentLevel = -1; // Auto
            }
        });
    }

    // Resume Watching
    async function loadResumePosition() {
        if (!movieId) return;

        try {
            const response = await fetch(`/api/watch-progress/${movieId}?episodeNumber=${episodeNumber || ''}`);
            if (!response.ok) return;

            const data = await response.json();
            if (data.hasProgress && data.currentTime > 5) { // Chỉ resume nếu đã xem > 5 giây
                const resume = confirm(`Bạn đã xem đến ${formatTime(data.currentTime)}. Bạn có muốn tiếp tục xem từ đây không?`);
                if (resume) {
                    video.currentTime = data.currentTime;
                }
            }
        } catch (error) {
            console.error('Error loading resume position:', error);
        }
    }

    // Save progress
    async function saveProgress() {
        if (!movieId || !video.duration || video.duration === 0) return;

        const currentTime = video.currentTime;
        const duration = video.duration;

        // Chỉ lưu nếu thay đổi đáng kể (> 5 giây)
        if (Math.abs(currentTime - lastSavedTime) < 5) return;

        try {
            await fetch('/api/watch-progress/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    movieId: movieId,
                    currentTime: currentTime,
                    duration: duration,
                    episodeNumber: episodeNumber
                })
            });
            lastSavedTime = currentTime;
        } catch (error) {
            console.error('Error saving progress:', error);
        }
    }

    // Picture-in-Picture
    function initPictureInPicture() {
        const pipBtn = document.getElementById('pip-btn');
        if (!pipBtn || !document.pictureInPictureEnabled) {
            if (pipBtn) pipBtn.style.display = 'none';
            return;
        }

        pipBtn.addEventListener('click', async function() {
            try {
                if (document.pictureInPictureElement) {
                    await document.exitPictureInPicture();
                } else {
                    await video.requestPictureInPicture();
                }
            } catch (error) {
                console.error('PiP error:', error);
            }
        });

        video.addEventListener('enterpictureinpicture', function() {
            pipBtn.textContent = '⛶ Thoát PiP';
        });

        video.addEventListener('leavepictureinpicture', function() {
            pipBtn.textContent = '⛶ Picture-in-Picture';
        });
    }

    // Format time helper
    function formatTime(seconds) {
        const h = Math.floor(seconds / 3600);
        const m = Math.floor((seconds % 3600) / 60);
        const s = Math.floor(seconds % 60);
        if (h > 0) {
            return `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
        }
        return `${m}:${s.toString().padStart(2, '0')}`;
    }

    // Initialize when video is ready
    video.addEventListener('loadedmetadata', function() {
        loadResumePosition();
        
        // Start saving progress periodically
        progressSaveInterval = setInterval(saveProgress, SAVE_INTERVAL);
        
        // Save on pause/seek
        video.addEventListener('pause', saveProgress);
        video.addEventListener('seeked', saveProgress);
        
        // Save before page unload
        window.addEventListener('beforeunload', saveProgress);
    });

    // Initialize PiP button
    initPictureInPicture();

    // Handle source changes (when user switches server/quality)
    const sourceButtons = document.querySelectorAll('.source-btn');
    sourceButtons.forEach(btn => {
        btn.addEventListener('click', function() {
            const url = this.getAttribute('data-url');
            if (url) {
                initHLS(url);
                // Reset progress save
                lastSavedTime = 0;
            }
        });
    });

    // Export functions for external use
    window.videoPlayerEnhanced = {
        saveProgress: saveProgress,
        loadResumePosition: loadResumePosition,
        initHLS: initHLS
    };

})();

