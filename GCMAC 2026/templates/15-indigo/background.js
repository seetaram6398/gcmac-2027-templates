(function () {
  "use strict";

  var canvas = document.getElementById("bg-canvas");
  if (!canvas) return;

  var ctx = canvas.getContext("2d");
  if (!ctx) return;

  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var dpr = Math.min(window.devicePixelRatio || 1, 2);
  var width = 0;
  var height = 0;
  var particles = [];
  var mouse = { x: null, y: null, radius: 180 };

  function Particle(x, y) {
    this.x = x;
    this.y = y;
    this.anchorX = x;
    this.anchorY = y;
    this.vx = (Math.random() - 0.5) * 0.45;
    this.vy = (Math.random() - 0.5) * 0.45;
    this.size = Math.random() * 2 + 1.2;
    this.tint = Math.random();
    this.phase = Math.random() * Math.PI * 2;
  }

  Particle.prototype.draw = function () {
    ctx.beginPath();
    ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
    ctx.fillStyle = this.tint > 0.55
      ? "rgba(108, 166, 214, 0.6)"
      : "rgba(61, 71, 133, 0.55)";
    ctx.fill();
  };

  Particle.prototype.update = function (time) {
    if (!reduceMotion) {
      this.anchorX += this.vx;
      this.anchorY += this.vy;

      if (this.anchorX < 0 || this.anchorX > width) this.vx *= -1;
      if (this.anchorY < 0 || this.anchorY > height) this.vy *= -1;

      this.anchorX += Math.sin(time * 0.0004 + this.phase) * 0.06;
    }

    var dx = mouse.x - this.x;
    var dy = mouse.y - this.y;
    var distance = mouse.x !== null ? Math.sqrt(dx * dx + dy * dy) : 9999;

    if (distance < mouse.radius && distance > 0.01) {
      var force = (mouse.radius - distance) / mouse.radius;
      this.x += (dx / distance) * force * 2.2;
      this.y += (dy / distance) * force * 2.2;
    } else {
      this.x += (this.anchorX - this.x) * 0.045;
      this.y += (this.anchorY - this.y) * 0.045;
    }
  };

  function init() {
    particles = [];
    var count = Math.min(140, Math.floor((width * height) / 11000));
    for (var i = 0; i < count; i++) {
      particles.push(new Particle(Math.random() * width, Math.random() * height));
    }
  }

  function connect() {
    for (var a = 0; a < particles.length; a++) {
      for (var b = a + 1; b < particles.length; b++) {
        var dx = particles[a].x - particles[b].x;
        var dy = particles[a].y - particles[b].y;
        var distance = Math.sqrt(dx * dx + dy * dy);

        if (distance < 120) {
          var opacity = 1 - distance / 120;
          ctx.strokeStyle = "rgba(108, 166, 214, " + (opacity * 0.45) + ")";
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.moveTo(particles[a].x, particles[a].y);
          ctx.lineTo(particles[b].x, particles[b].y);
          ctx.stroke();
        }
      }
    }
  }

  function draw(time) {
    ctx.clearRect(0, 0, width, height);
    for (var i = 0; i < particles.length; i++) {
      particles[i].update(time);
      particles[i].draw();
    }
    connect();
  }

  function frame(time) {
    draw(time);
    if (!reduceMotion) requestAnimationFrame(frame);
  }

  function resize() {
    width = window.innerWidth;
    height = window.innerHeight;
    canvas.width = Math.round(width * dpr);
    canvas.height = Math.round(height * dpr);
    canvas.style.width = width + "px";
    canvas.style.height = height + "px";
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    init();
    draw(performance.now());
  }

  window.addEventListener("mousemove", function (event) {
    mouse.x = event.clientX;
    mouse.y = event.clientY;
  });
  window.addEventListener("mouseout", function () {
    mouse.x = null;
    mouse.y = null;
  });
  window.addEventListener("touchmove", function (event) {
    if (event.touches.length) {
      mouse.x = event.touches[0].clientX;
      mouse.y = event.touches[0].clientY;
    }
  }, { passive: true });
  window.addEventListener("touchend", function () {
    mouse.x = null;
    mouse.y = null;
  });
  window.addEventListener("resize", resize);

  var heroVideo = document.querySelector(".hero-video");
  if (heroVideo) {
    if (reduceMotion) {
      heroVideo.autoplay = false;
      heroVideo.pause();
    } else if ("IntersectionObserver" in window) {
      var videoObserver = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            var playAttempt = heroVideo.play();
            if (playAttempt && playAttempt.catch) playAttempt.catch(function () {});
          } else {
            heroVideo.pause();
          }
        });
      }, { threshold: 0.05 });
      videoObserver.observe(heroVideo);
    }
  }

  resize();
  if (!reduceMotion) requestAnimationFrame(frame);
})();
