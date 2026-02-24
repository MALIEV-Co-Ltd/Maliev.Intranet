/*
 * Robust Simplex Noise implementation for organic flow field animation
 * Ported from various open source implementations for high performance JS canvas
 */

// Simplex Noise implementation
const SimplexNoise = (function () {
    var F2 = 0.5 * (Math.sqrt(3.0) - 1.0);
    var G2 = (3.0 - Math.sqrt(3.0)) / 6.0;
    var Grad = [
        [1, 1], [-1, 1], [1, -1], [-1, -1],
        [1, 0], [-1, 0], [1, 0], [-1, 0],
        [0, 1], [0, -1], [0, 1], [0, -1]
    ];
    var p = new Uint8Array(256);
    var perm = new Uint8Array(512);
    var permMod12 = new Uint8Array(512);

    for (var i = 0; i < 256; i++) {
        p[i] = Math.floor(Math.random() * 256);
    }
    for (i = 0; i < 512; i++) {
        perm[i] = p[i & 255];
        permMod12[i] = perm[i] % 12;
    }

    return {
        noise2D: function (xin, yin) {
            var n0, n1, n2; // Noise contributions from the three corners
            // Skew the input space to determine which simplex cell we're in
            var s = (xin + yin) * F2; // Hairy factor for 2D
            var i = Math.floor(xin + s);
            var j = Math.floor(yin + s);
            var t = (i + j) * G2;
            var X0 = i - t; // Unskew the cell origin back to (x,y) space
            var Y0 = j - t;
            var x0 = xin - X0; // The x,y distances from the cell origin
            var y0 = yin - Y0;
            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            var i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else { i1 = 0; j1 = 1; } // upper triangle, YX order: (0,0)->(0,1)->(1,1)
            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6
            var x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
            var y1 = y0 - j1 + G2;
            var x2 = x0 - 1.0 + 2.0 * G2; // Offsets for last corner in (x,y) unskewed coords
            var y2 = y0 - 1.0 + 2.0 * G2;
            // Work out the hashed gradient indices of the three simplex corners
            var ii = i & 255;
            var jj = j & 255;
            var gi0 = permMod12[ii + perm[jj]];
            var gi1 = permMod12[ii + i1 + perm[jj + j1]];
            var gi2 = permMod12[ii + 1 + perm[jj + 1]];
            // Calculate the contribution from the three corners
            var t0 = 0.5 - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0;
            else {
                t0 *= t0;
                n0 = t0 * t0 * (Grad[gi0][0] * x0 + Grad[gi0][1] * y0);
            }
            var t1 = 0.5 - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0;
            else {
                t1 *= t1;
                n1 = t1 * t1 * (Grad[gi1][0] * x1 + Grad[gi1][1] * y1);
            }
            var t2 = 0.5 - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0;
            else {
                t2 *= t2;
                n2 = t2 * t2 * (Grad[gi2][0] * x2 + Grad[gi2][1] * y2);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 70.0 * (n0 + n1 + n2);
        }
    };
})();

export function initLoginBg() {
    const canvas = document.getElementById('login-canvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    let width, height;
    let particles = [];
    let animationId;

    // Configuration
    const particleCount = 1500; // Optimal count for performance/visual balance
    const noiseScale = 0.003; // Scale of the noise pattern (smaller = larger features)
    const timeScale = 0.0002; // Speed of noise evolution
    const trailFade = 0.05; // Opacity of trail fade (lower = longer trails)

    let mouse = { x: -1000, y: -1000 };

    class Particle {
        constructor() {
            this.reset(true);
        }

        reset(randomValues = false) {
            this.x = randomValues ? Math.random() * width : Math.random() * width;
            this.y = randomValues ? Math.random() * height : Math.random() * height;
            this.vx = 0;
            this.vy = 0;
            this.age = 0;
            this.lifeSpan = Math.random() * 300 + 100;
            this.size = Math.random() * 1.5 + 0.5;
            this.updateColor();
        }

        updateColor() {
            const isDark = !document.documentElement.getAttribute('data-theme') || document.documentElement.getAttribute('data-theme') === 'dark';
            // Use softer, more diverse colors for organic feel
            if (isDark) {
                // Cyberpunk/Neon palette: Cyan, Magenta, Deep Purple
                const hue = Math.random() > 0.5 ? 180 + Math.random() * 60 : 280 + Math.random() * 60;
                this.color = `hsla(${hue}, 80%, 60%, 0.8)`;
            } else {
                // Light mode: Blue, Indigo, Teal
                const hue = 190 + Math.random() * 60;
                this.color = `hsla(${hue}, 70%, 50%, 0.6)`;
            }
        }

        update(time) {
            this.age++;
            if (this.age > this.lifeSpan) {
                this.reset();
            }

            // Get noise value based on position (2D noise + time for evolution)
            // Range roughly -1 to 1
            const n = SimplexNoise.noise2D(this.x * noiseScale, this.y * noiseScale + time * timeScale);
            // Map noise to angle (0 to 2*PI * 2 for more swirls)
            const angle = n * Math.PI * 4;

            // Base velocity from noise
            this.vx += Math.cos(angle) * 0.2; // Acceleration for smooth turns
            this.vy += Math.sin(angle) * 0.2;

            // Viscosity/Friction to prevent unlimited speed
            this.vx *= 0.95;
            this.vy *= 0.95;

            // Apply velocity
            this.x += this.vx;
            this.y += this.vy;

            // Mouse Interaction (Repulsion/Attraction)
            const dx = mouse.x - this.x;
            const dy = mouse.y - this.y;
            const dist = Math.sqrt(dx * dx + dy * dy);

            if (dist < 200) {
                const angleToMouse = Math.atan2(dy, dx);
                const force = (200 - dist) / 200;
                // Swirl around mouse
                this.vx -= Math.cos(angleToMouse + Math.PI / 2) * force * 0.5;
                this.vy -= Math.sin(angleToMouse + Math.PI / 2) * force * 0.5;
                // Slight attraction
                this.vx += Math.cos(angleToMouse) * force * 0.1;
                this.vy += Math.sin(angleToMouse) * force * 0.1;
            }

            // Boundary wrap
            if (this.x < 0) this.x = width;
            if (this.x > width) this.x = 0;
            if (this.y < 0) this.y = height;
            if (this.y > height) this.y = 0;
        }

        draw() {
            ctx.fillStyle = this.color;
            ctx.beginPath();
            ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    function resize() {
        width = canvas.width = window.innerWidth;
        height = canvas.height = window.innerHeight;
    }

    function init() {
        resize();
        particles = [];
        for (let i = 0; i < particleCount; i++) {
            particles.push(new Particle());
        }
    }

    let time = 0;
    let lastTheme = document.documentElement.getAttribute('data-theme') || 'dark';

    function animate() {
        if (ctx) {
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
            const isDark = !currentTheme || currentTheme === 'dark';

            // Detect theme switch
            if (currentTheme !== lastTheme) {
                lastTheme = currentTheme;
                // Update particles color target, but let the trail effect handle the background transition naturally
                particles.forEach(p => p.updateColor());
            }

            // Trail effect (draw semi-transparent rect over previous frame)
            ctx.fillStyle = isDark ? `rgba(15, 23, 42, ${trailFade})` : `rgba(240, 249, 255, ${trailFade})`;
            ctx.fillRect(0, 0, width, height);

            time++;
            particles.forEach(p => {
                p.update(time);
                p.draw();
            });
        }
        animationId = requestAnimationFrame(animate);
    }

    window.addEventListener('resize', () => {
        const oldWidth = width;
        const oldHeight = height;

        resize();

        // Scale particle positions instead of re-initializing to preserve state
        if (oldWidth > 0 && oldHeight > 0) {
            const scaleX = width / oldWidth;
            const scaleY = height / oldHeight;

            particles.forEach(p => {
                p.x *= scaleX;
                p.y *= scaleY;
                // Ensure they stay within bounds just in case
                if (p.x > width) p.x = width;
                if (p.y > height) p.y = height;
            });
        }
    });

    window.addEventListener('mousemove', e => {
        mouse.x = e.clientX;
        mouse.y = e.clientY;
    });

    window.addEventListener('mouseout', () => {
        mouse.x = -1000;
        mouse.y = -1000;
    });

    // Clean up previous animations if re-initialized
    // (Note: Since we use requestAnimationFrame recursively, 
    // we rely on garbage collection of the old closure if module reloads, 
    // or manually canceling if we stored the ID globally, which is harder in modules without state.
    // However, for typical page loads, this runs once.)

    init();
    animate();
}
