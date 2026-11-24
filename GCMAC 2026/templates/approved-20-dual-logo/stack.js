(function () {
  "use strict";

  var el = document.querySelector(".typewriter-text");
  if (!el) return;

  var text = el.getAttribute("data-text") || el.textContent || "";
  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  if (reduceMotion || !text) {
    el.textContent = text;
    return;
  }

  var speed = 60;
  var eraseSpeed = 30;
  var hold = 2000;
  var i = 0;
  var deleting = false;

  el.textContent = "";

  function typeEffect() {
    if (!deleting) {
      el.textContent = text.substring(0, i + 1);
      i++;
      if (i === text.length) {
        deleting = true;
        setTimeout(typeEffect, hold);
        return;
      }
    } else {
      el.textContent = text.substring(0, i - 1);
      i--;
      if (i === 0) deleting = false;
    }
    setTimeout(typeEffect, deleting ? eraseSpeed : speed);
  }

  typeEffect();
})();

(function () {
  "use strict";

  var counters = document.querySelectorAll(".stat-item strong[data-count]");
  if (!counters.length) return;

  var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function showFinal(node) {
    var target = parseInt(node.getAttribute("data-count"), 10) || 0;
    var suffix = node.getAttribute("data-suffix") || "";
    node.textContent = target + suffix;
  }

  if (reduceMotion || !("IntersectionObserver" in window)) {
    Array.prototype.forEach.call(counters, showFinal);
    return;
  }

  function animate(node) {
    var target = parseInt(node.getAttribute("data-count"), 10) || 0;
    var suffix = node.getAttribute("data-suffix") || "";
    var duration = 1400;
    var start = null;

    function step(now) {
      if (start === null) start = now;
      var progress = Math.min((now - start) / duration, 1);
      var eased = 1 - Math.pow(1 - progress, 3);
      node.textContent = Math.round(target * eased) + suffix;
      if (progress < 1) window.requestAnimationFrame(step);
    }

    window.requestAnimationFrame(step);
  }

  Array.prototype.forEach.call(counters, function (node) {
    node.textContent = "0" + (node.getAttribute("data-suffix") || "");
  });

  var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (!entry.isIntersecting) return;
      animate(entry.target);
      observer.unobserve(entry.target);
    });
  }, { threshold: 0.6 });

  Array.prototype.forEach.call(counters, function (node) {
    observer.observe(node);
  });
})();
