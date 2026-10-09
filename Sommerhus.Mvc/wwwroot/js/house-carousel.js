// Image browsing for house cards and the house page. An element with data-carousel lists its
// images in data-carousel-images (JSON: [{ src, alt }]); the arrows step through them, wrapping
// at the ends, and a thumbnail jumps to its image.
(function () {
    'use strict';

    function setUp(root) {
        let images;
        try {
            images = JSON.parse(root.getAttribute('data-carousel-images') || '[]');
        } catch {
            return;
        }

        const image = root.querySelector('[data-carousel-image]');
        if (!image || images.length < 2) {
            return;
        }

        const counter = root.querySelector('[data-carousel-counter]');
        const thumbs = Array.from(root.querySelectorAll('[data-carousel-thumb]'));
        let current = 0;

        function show(index) {
            current = (index + images.length) % images.length;
            image.src = images[current].src;
            image.alt = images[current].alt || '';
            if (counter) {
                counter.textContent = (current + 1) + ' / ' + images.length;
            }
            thumbs.forEach(function (thumb, i) {
                thumb.setAttribute('aria-pressed', i === current ? 'true' : 'false');
            });
        }

        const prev = root.querySelector('[data-carousel-prev]');
        const next = root.querySelector('[data-carousel-next]');
        if (prev) {
            prev.addEventListener('click', function () { show(current - 1); });
        }
        if (next) {
            next.addEventListener('click', function () { show(current + 1); });
        }
        thumbs.forEach(function (thumb) {
            thumb.addEventListener('click', function () {
                show(Number(thumb.getAttribute('data-carousel-thumb')));
            });
        });
    }

    document.querySelectorAll('[data-carousel]').forEach(setUp);
})();
