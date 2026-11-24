(function () {
  "use strict";

  var hero = document.querySelector(".hero-slider");
  if (!hero) return;

  var slides = Array.prototype.slice.call(hero.querySelectorAll(".hero-slide"));
  var dots = Array.prototype.slice.call(hero.querySelectorAll(".hero-dot"));
  var prev = hero.querySelector(".hero-nav-prev");
  var next = hero.querySelector(".hero-nav-next");
  if (slides.length < 2) return;

  var index = 0;
  var timer = null;
  var delay = 5500;
  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function show(target) {
    index = (target + slides.length) % slides.length;
    slides.forEach(function (slide, i) {
      slide.classList.toggle("is-active", i === index);
    });
    dots.forEach(function (dot, i) {
      var active = i === index;
      dot.classList.toggle("is-active", active);
      if (active) {
        dot.setAttribute("aria-current", "true");
      } else {
        dot.removeAttribute("aria-current");
      }
    });
  }

  function stop() {
    if (timer) {
      clearInterval(timer);
      timer = null;
    }
  }

  function start() {
    stop();
    if (!reduceMotion) timer = setInterval(function () { show(index + 1); }, delay);
  }

  function go(target) {
    show(target);
    start();
  }

  if (prev) prev.addEventListener("click", function () { go(index - 1); });
  if (next) next.addEventListener("click", function () { go(index + 1); });
  dots.forEach(function (dot, i) {
    dot.addEventListener("click", function () { go(i); });
  });

  hero.addEventListener("mouseenter", stop);
  hero.addEventListener("mouseleave", start);
  hero.addEventListener("focusin", stop);
  hero.addEventListener("focusout", start);
  document.addEventListener("visibilitychange", function () {
    if (document.hidden) {
      stop();
    } else {
      start();
    }
  });

  var touchStartX = null;
  hero.addEventListener("touchstart", function (event) {
    if (event.touches.length) touchStartX = event.touches[0].clientX;
  }, { passive: true });
  hero.addEventListener("touchend", function (event) {
    if (touchStartX === null || !event.changedTouches.length) return;
    var delta = event.changedTouches[0].clientX - touchStartX;
    if (Math.abs(delta) > 40) go(index + (delta < 0 ? 1 : -1));
    touchStartX = null;
  }, { passive: true });

  show(0);
  start();
})();
